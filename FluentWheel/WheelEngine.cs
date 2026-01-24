using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using System.Collections.Concurrent;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace Cloris.FluentWheel;

internal static class WheelEngine
{
    private const int WM_MOUSEWHEEL = 0x020A;
    private const int MK_CONTROL = 0x0008;

    private static readonly ConcurrentQueue<IWpfTextView> _pendingViews = [];
    private static readonly HashSet<TextViewAnimationState> _activeAnimationStates = [];

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

        CompositionTarget.Rendering += FrameRendering;
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
        if (view.VisualElement.IsInitialized)
        {
            InitializeViewComponents(view);
        }
        else
        {
            view.VisualElement.Initialized += delegate
            {
                InitializeViewComponents(view);
            };
        }
    }

    private static void InitializeViewComponents(IWpfTextView view)
    {
        var innerViewScrollField = view.GetType().GetField("_viewScroller", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (innerViewScrollField is not null && typeof(IViewScroller).IsAssignableFrom(innerViewScrollField.FieldType))
        {
            var animationState = new TextViewAnimationState(view);
            innerViewScrollField.SetValue(view, animationState.ViewScroller);
            HookWindowMessages(animationState);
        }
    }

    private static void HookWindowMessages(TextViewAnimationState animationState)
    {
        HwndSource? currentSource = null;

        nint Hook(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
        {
            if (msg == WM_MOUSEWHEEL
                && (wParam & MK_CONTROL) == MK_CONTROL
                && !animationState.View.IsClosed
                && VisualTreeHelper.HitTest(animationState.View.VisualElement, Mouse.GetPosition(animationState.View.VisualElement)) is not null)
            {
                var delta = (int)wParam >> 16;
                var scale = delta > 0 ? delta / 1200.0 : delta / 1320.0;
                animationState.ZoomAnimation.Zoom(animationState.View.ZoomLevel, scale, Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt));
                _activeAnimationStates.Add(animationState);
                handled = true;
            }

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
            if (args.OldSource is HwndSource { IsDisposed: false } oldSource)
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
        _activeAnimationStates.RemoveWhere(animationState => !animationState.IsAnimating || animationState.View.IsClosed);

        foreach (var animationState in _activeAnimationStates)
        {
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
                var zoomLevel = animationState.ZoomAnimation.CalculateZoom();
                animationState.View.Options.GlobalOptions.SetOptionValue(DefaultWpfViewOptions.ZoomLevelId, zoomLevel);
            }
        }
    }

    public static void HorizontalScroll(TextViewAnimationState animationState, double distance)
    {
        if (!animationState.View.IsClosed)
        {
            animationState.HorizontalScrollAnimation.Scroll(distance);
            _activeAnimationStates.Add(animationState);
        }
    }

    public static void VerticalScroll(TextViewAnimationState animationState, double distance)
    {
        if (!animationState.View.IsClosed)
        {
            animationState.VerticalScrollAnimation.Scroll(distance);
            _activeAnimationStates.Add(animationState);
        }
    }

    public static void Cleanup()
    {
        CompositionTarget.Rendering -= FrameRendering;
        _activeAnimationStates.Clear();
        IsInitialized = false;
    }
}
