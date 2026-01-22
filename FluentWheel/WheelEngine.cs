using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
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
    private static readonly ConditionalWeakTable<IWpfTextView, TextViewAnimationState> _viewAnimationStates = new ();
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
        var viewScroller = new ViewScroller(view);

        var innerViewScrollField = view.GetType().GetField("_viewScroller", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (innerViewScrollField is not null && typeof(IViewScroller).IsAssignableFrom(innerViewScrollField.FieldType))
        {
            innerViewScrollField.SetValue(view, viewScroller);
            _viewAnimationStates.Add(view, new TextViewAnimationState(view, viewScroller));

            HookWindowMessages(view);
        }
    }

    private static void HookWindowMessages(IWpfTextView view)
    {
        HwndSource? currentSource = null;

        nint Hook(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
        {
            if (msg != WM_MOUSEWHEEL
                || (wParam & MK_CONTROL) != MK_CONTROL
                || VisualTreeHelper.HitTest(view.VisualElement, Mouse.GetPosition(view.VisualElement)) is null
                || view is null or { IsClosed: true }
                || !_viewAnimationStates.TryGetValue(view, out var animationState))
            {
                return default;
            }

            var delta = (int)wParam >> 16;
            var scale = delta > 0 ? delta / 1200.0 : delta / 1320.0;
            animationState.ZoomAnimation.Zoom(animationState.View.ZoomLevel, scale, Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt));
            _activeAnimationStates.Add(animationState);
            handled = true;
            return default;
        }

        void Attach(HwndSource source)
        {
            if (source is { IsDisposed: false } && currentSource != source)
            {
                currentSource?.RemoveHook(Hook); source.AddHook(Hook); currentSource = source;
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

            if (!view.IsClosed && args.NewSource is HwndSource newSource)
            {
                Attach(newSource);
            }

            if (view.IsClosed)
            {
                Detach();
                PresentationSource.RemoveSourceChangedHandler(view.VisualElement, SourceChanged);
            }
        }

        if (PresentationSource.FromVisual(view.VisualElement) is HwndSource initialSource)
        {
            Attach(initialSource);
        }

        PresentationSource.AddSourceChangedHandler(view.VisualElement, SourceChanged);
    }

    private static void FrameRendering(object sender, EventArgs e)
    {
        _activeAnimationStates.RemoveWhere(animationState => !animationState.IsAnimating || animationState.View is null or { IsClosed: true });

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

    public static void HorizontalScroll(IWpfTextView view, double distance)
    {
        if (_viewAnimationStates.TryGetValue(view, out var animationState) && animationState.View is { IsClosed: false })
        {
            animationState.HorizontalScrollAnimation.Scroll(distance);
            _activeAnimationStates.Add(animationState);
        }
    }

    public static void VerticalScroll(IWpfTextView view, double distance)
    {
        if (view is { IsClosed: false } && _viewAnimationStates.TryGetValue(view, out var animationState))
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
