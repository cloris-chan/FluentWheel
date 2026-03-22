using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using System.Collections.Concurrent;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace Cloris.FluentWheel;

internal static class WheelEngine
{
    private static readonly ConcurrentQueue<IWpfTextView> _pendingViews = [];
    private static readonly HashSet<TextViewAnimationState> _activeAnimationStates = [];
    private static readonly HashSet<TextViewAnimationState> _registeredAnimationStates = [];
    private static readonly ConcurrentQueue<LowLevelMouseHook.MouseWheelInput> _pendingLowLevelMouseWheelInputs = [];

    public static bool IsInitialized { get; private set; }

    public static async Task InitializeAsync()
    {
        if (!ThreadHelper.JoinableTaskContext.IsOnMainThread)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        }

        while (_pendingViews.TryDequeue(out var view))
        {
            RegisterViewInternal(view);
        }

        LowLevelMouseHook.MouseWheelInputReceived += _pendingLowLevelMouseWheelInputs.Enqueue;
        SettingsCache.SettingsChanged += SettingsChanged;
        CompositionTarget.Rendering += FrameRendering;

        SettingsChanged(nameof(SettingsCache.EnableLowLevelMouseHook));

        IsInitialized = true;
    }

    public static void RegisterView(IWpfTextView view)
    {
        if (SettingsCache.IsInitialized)
        {
            RegisterViewInternal(view);
        }
        else
        {
            _pendingViews.Enqueue(view);
        }
    }

    private static void RegisterViewInternal(IWpfTextView view)
    {
        if (view.VisualElement.IsLoaded)
        {
            InitializeViewComponents(view);
        }
        else
        {
            view.VisualElement.Loaded += VisualElement_Loaded;

            void VisualElement_Loaded(object sender, RoutedEventArgs e)
            {
                view.VisualElement.Loaded -= VisualElement_Loaded;
                InitializeViewComponents(view);
            }
        }
    }

    private static void InitializeViewComponents(IWpfTextView view)
    {
        try
        {
            var innerViewScrollField = view.GetType()
                .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Single(field => typeof(IViewScroller).IsAssignableFrom(field.FieldType));

            var animationState = new TextViewAnimationState(view);
            _registeredAnimationStates.Add(animationState);
            innerViewScrollField.SetValue(view, animationState.ViewScroller);
            view.Closed += delegate
            {
                _registeredAnimationStates.Remove(animationState);
                _activeAnimationStates.Remove(animationState);
            };
            HookWindowMessages(animationState);
        }
        catch (InvalidOperationException) { }
    }

    private static void SettingsChanged(string propertyName)
    {
        if (propertyName is nameof(SettingsCache.EnableLowLevelMouseHook))
        {
            if (SettingsCache.EnableLowLevelMouseHook)
            {
                _ = LowLevelMouseHook.EnableAsync();
            }
            else
            {
                _ = LowLevelMouseHook.DisableAsync();
            }
        }
    }

    private static void HookWindowMessages(TextViewAnimationState animationState)
    {
        const int WM_MOUSEWHEEL = 0x020A;
        const int WM_MOUSEHWHEEL = 0x020E;
        const int MK_CONTROL = 0x0008;
        const int MK_SHIFT = 0x0004;

        HwndSource? currentSource = null;

        nint Hook(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
        {
            if (msg != WM_MOUSEWHEEL && msg != WM_MOUSEHWHEEL)
            {
                return default;
            }

            if (!IsMouseOverHost(animationState))
            {
                return default;
            }


            if (LowLevelMouseHook.IsEnabled)
            {
                handled = true;
                return default;
            }

            var delta = (int)wParam >> 16;
            if (msg == WM_MOUSEWHEEL && (wParam & MK_CONTROL) == MK_CONTROL && animationState.CanZoom)
            {
                var scale = delta > 0 ? delta / 1200.0 : delta / 1320.0;
                animationState.ZoomAnimation.Zoom(animationState.View.ZoomLevel, scale, Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt));
            }
            else if (msg == WM_MOUSEHWHEEL)
            {
                animationState.HorizontalScrollAnimation.Scroll(GetScrollDistance(animationState, delta));
            }
            else if ((wParam & MK_SHIFT) == MK_SHIFT)
            {
                animationState.HorizontalScrollAnimation.Scroll(-GetScrollDistance(animationState, delta));
            }
            else
            {
                animationState.VerticalScrollAnimation.Scroll(GetScrollDistance(animationState, delta));
            }

            _activeAnimationStates.Add(animationState);
            handled = true;

            return default;
        }

        void Attach(HwndSource source)
        {
            if (source is { IsDisposed: false } && currentSource != source)
            {
                currentSource?.RemoveHook(Hook);
                source.AddHook(Hook);
                currentSource = source;
            }
        }

        void Detach()
        {
            if (currentSource is { IsDisposed: false })
            {
                currentSource.RemoveHook(Hook);
            }

            currentSource = null;
        }

        void SourceChanged(object? sender, SourceChangedEventArgs args)
        {
            if (args.OldSource is HwndSource { IsDisposed: false })
            {
                Detach();
            }

            if (!animationState.View.IsClosed && args.NewSource is HwndSource newSource)
            {
                Attach(newSource);
            }

            if (animationState.View.IsClosed)
            {
                Detach();
                PresentationSource.RemoveSourceChangedHandler(animationState.View.VisualElement, SourceChanged);
            }
        }

        if (PresentationSource.FromVisual(animationState.View.VisualElement) is HwndSource initialSource)
        {
            Attach(initialSource);
        }

        PresentationSource.AddSourceChangedHandler(animationState.View.VisualElement, SourceChanged);
    }

    private static void FrameRendering(object sender, EventArgs e)
    {
        ProcessPendingLowLevelMouseWheelInputs();
        _activeAnimationStates.RemoveWhere(animationState => !animationState.IsAnimating || animationState.View.IsClosed);

        foreach (var animationState in _activeAnimationStates)
        {
            if (!animationState.CanAnimate)
            {
                continue;
            }

            if (animationState.VerticalScrollAnimation.IsAnimating)
            {
                var distance = animationState.VerticalScrollAnimation.CalculateDistance();
                animationState.ViewScroller.VerticallyScroll(distance);
            }

            if (animationState.HorizontalScrollAnimation.IsAnimating)
            {
                var distance = animationState.HorizontalScrollAnimation.CalculateDistance();
                animationState.ViewScroller.HorizontallyScroll(distance);
            }

            if (animationState.ZoomAnimation.IsAnimating)
            {
                var zoomLevel = animationState.ZoomAnimation.CalculateZoomLevel();
                animationState.View.Options.GlobalOptions.SetOptionValue(DefaultWpfViewOptions.ZoomLevelId, zoomLevel);
            }
        }
    }

    public static void HorizontalScroll(TextViewAnimationState animationState, double distance)
    {
        if (animationState.CanAnimate)
        {
            animationState.HorizontalScrollAnimation.Scroll(distance);
            _activeAnimationStates.Add(animationState);
        }
    }

    public static void VerticalScroll(TextViewAnimationState animationState, double distance)
    {
        if (animationState.CanAnimate)
        {
            animationState.VerticalScrollAnimation.Scroll(distance);
            _activeAnimationStates.Add(animationState);
        }
    }

    public static void Cleanup()
    {
        CompositionTarget.Rendering -= FrameRendering;
        SettingsCache.SettingsChanged -= SettingsChanged;
        LowLevelMouseHook.MouseWheelInputReceived -= _pendingLowLevelMouseWheelInputs.Enqueue;
        _ = LowLevelMouseHook.DisableAsync();

        _activeAnimationStates.Clear();
        _registeredAnimationStates.Clear();
        while (_pendingLowLevelMouseWheelInputs.TryDequeue(out _))
        {
        }

        IsInitialized = false;
    }

    private static void ProcessPendingLowLevelMouseWheelInputs()
    {
        while (_pendingLowLevelMouseWheelInputs.TryDequeue(out var input))
        {
            if (!TryGetHoveredAnimationState(out var animationState))
            {
                continue;
            }

            if (input is { IsControlPressed: true, IsHorizontal: false } && animationState.CanZoom)
            {
                var scale = input.Delta > 0 ? input.Delta / 1200.0 : input.Delta / 1320.0;
                animationState.ZoomAnimation.Zoom(animationState.View.ZoomLevel, scale, input.IsAltPressed);
            }
            else if (input.IsHorizontal)
            {
                animationState.HorizontalScrollAnimation.Scroll(GetScrollDistance(animationState, input.Delta));
            }
            else if (input.IsShiftPressed)
            {
                animationState.HorizontalScrollAnimation.Scroll(-GetScrollDistance(animationState, input.Delta));
            }
            else
            {
                animationState.VerticalScrollAnimation.Scroll(GetScrollDistance(animationState, input.Delta));
            }

            _activeAnimationStates.Add(animationState);
        }
    }

    private static bool TryGetHoveredAnimationState(out TextViewAnimationState animationState)
    {
        foreach (var state in _registeredAnimationStates)
        {
            if (IsMouseOverHost(state))
            {
                animationState = state;
                return true;
            }
        }

        animationState = null!;
        return false;
    }

    private static bool IsMouseOverHost(TextViewAnimationState animationState)
    {
        return animationState is { View.IsClosed: false, Host.HostControl.IsMouseOver: true };
    }

    private static double GetScrollDistance(TextViewAnimationState animationState, int delta)
    {
        const double WheelDelta = 120.0;
        var baseDistance = SystemParameters.WheelScrollLines > 0
            ? delta / WheelDelta * animationState.View.LineHeight * SystemParameters.WheelScrollLines
            : delta / WheelDelta * animationState.View.LineHeight * 3;
        return baseDistance * SettingsCache.VerticalScrollRate / 100.0;
    }
}
