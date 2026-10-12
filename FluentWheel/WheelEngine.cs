using System.Collections.Concurrent;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;

namespace Cloris.FluentWheel;

internal static class WheelEngine
{
    private static readonly ConcurrentQueue<IWpfTextView> _pendingViews = [];
    private static readonly HashSet<TextViewAnimationState> _activeAnimationStates = [];
    private static readonly HashSet<TextViewAnimationState> _registeredAnimationStates = [];
    private static readonly Dictionary<IWpfTextView, Action> _pendingViewCleanups = [];
    private static readonly Dictionary<TextViewAnimationState, Action> _viewCleanups = [];
    private static readonly Dictionary<HwndSource, WindowWheelHook> _windowHooks = [];
    private static bool _isRenderingSubscribed;

    public static bool IsInitialized { get; private set; }

    public static async Task InitializeAsync()
    {
        if (!ThreadHelper.JoinableTaskContext.IsOnMainThread)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        }

        if (IsInitialized)
        {
            return;
        }

        IsInitialized = true;

        while (_pendingViews.TryDequeue(out var view))
        {
            RegisterViewInternal(view);
        }
    }

    public static void RegisterView(IWpfTextView view)
    {
        if (IsInitialized)
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
        if (view.IsClosed || _pendingViewCleanups.ContainsKey(view) || _registeredAnimationStates.Any(state => state.View == view))
        {
            return;
        }

        if (view.VisualElement.IsLoaded)
        {
            InitializeViewComponents(view);
        }
        else
        {
            view.VisualElement.Loaded += VisualElement_Loaded;
            view.Closed += ViewClosed;
            _pendingViewCleanups.Add(view, Detach);

            void VisualElement_Loaded(object sender, RoutedEventArgs e)
            {
                RemovePendingView(view);
                if (IsInitialized && !view.IsClosed)
                {
                    InitializeViewComponents(view);
                }
            }

            void ViewClosed(object? sender, EventArgs args) => RemovePendingView(view);

            void Detach()
            {
                view.VisualElement.Loaded -= VisualElement_Loaded;
                view.Closed -= ViewClosed;
            }
        }
    }

    private static void RemovePendingView(IWpfTextView view)
    {
        if (_pendingViewCleanups.TryGetValue(view, out var cleanup))
        {
            _pendingViewCleanups.Remove(view);
            cleanup();
        }
    }

    private static void InitializeViewComponents(IWpfTextView view)
    {
        try
        {
            var innerViewScrollField = FindViewScrollerField(view);
            if (innerViewScrollField is null)
            {
                ExtensionDiagnostics.TraceWarning(nameof(InitializeViewComponents), $"Unable to locate the native IViewScroller field on {view.GetType().FullName}; using the window wheel hook only.");
            }

            var animationState = new TextViewAnimationState(view);
            Action? detachInput = null;
            var isViewScrollerReplaced = false;
            object? originalViewScroller = null;
            _viewCleanups.Add(animationState, Detach);
            try
            {
                _registeredAnimationStates.Add(animationState);
                if (innerViewScrollField is not null)
                {
                    try
                    {
                        originalViewScroller = innerViewScrollField.GetValue(view);
                        innerViewScrollField.SetValue(view, animationState.ViewScroller);
                        isViewScrollerReplaced = true;
                    }
                    catch (Exception ex)
                    {
                        ExtensionDiagnostics.TraceException("InitializeViewComponents.ReplaceScroller", ex);
                    }
                }

                view.Closed += ViewClosed;
                detachInput = HookWindowMessages(animationState);
            }
            catch
            {
                UnregisterView(animationState);
                throw;
            }

            void ViewClosed(object? sender, EventArgs args) => UnregisterView(animationState);

            void Detach()
            {
                view.Closed -= ViewClosed;
                detachInput?.Invoke();
                if (isViewScrollerReplaced && innerViewScrollField is not null && ReferenceEquals(innerViewScrollField.GetValue(view), animationState.ViewScroller))
                {
                    innerViewScrollField.SetValue(view, originalViewScroller);
                }
            }
        }
        catch (Exception ex)
        {
            ExtensionDiagnostics.TraceException(nameof(InitializeViewComponents), ex);
        }
    }

    private static FieldInfo? FindViewScrollerField(IWpfTextView view)
    {
        var nativeScroller = view.ViewScroller;
        var fields = new List<FieldInfo>();
        for (var type = view.GetType(); type is not null; type = type.BaseType)
        {
            fields.AddRange(type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(field => typeof(IViewScroller).IsAssignableFrom(field.FieldType)));
        }

        var matchingFields = fields.Where(field =>
        {
            try
            {
                return ReferenceEquals(field.GetValue(view), nativeScroller);
            }
            catch
            {
                return false;
            }
        }).ToArray();
        if (matchingFields.Length == 1)
        {
            return matchingFields[0];
        }

        return fields.Count == 1 ? fields[0] : null;
    }

    private static void UnregisterView(TextViewAnimationState animationState)
    {
        _registeredAnimationStates.Remove(animationState);
        _activeAnimationStates.Remove(animationState);
        try
        {
            if (_viewCleanups.TryGetValue(animationState, out var cleanup))
            {
                _viewCleanups.Remove(animationState);
                cleanup();
            }
        }
        catch (Exception ex)
        {
            ExtensionDiagnostics.TraceException("WheelEngine.UnregisterView", ex);
        }
        finally
        {
            animationState.Dispose();
            UpdateRenderingSubscription();
        }
    }

    private static Action HookWindowMessages(TextViewAnimationState animationState)
    {
        HwndSource? currentSource = null;

        void Attach(HwndSource source)
        {
            if (source is { IsDisposed: false } && currentSource != source)
            {
                Detach();
                if (!_windowHooks.TryGetValue(source, out var windowHook))
                {
                    windowHook = new WindowWheelHook(source);
                    windowHook.Attach();
                    _windowHooks.Add(source, windowHook);
                }

                currentSource = source;
                windowHook.AddView(animationState);
            }
        }

        void Detach()
        {
            if (currentSource is not null && _windowHooks.TryGetValue(currentSource, out var windowHook))
            {
                windowHook.RemoveView(animationState);
                if (windowHook.IsEmpty)
                {
                    windowHook.Detach();
                    _windowHooks.Remove(currentSource);
                }
            }

            currentSource = null;
        }

        void SourceChanged(object? sender, SourceChangedEventArgs args)
        {
            if (args.OldSource is HwndSource)
            {
                Detach();
            }

            if (!animationState.View.IsClosed && args.NewSource is HwndSource newSource)
            {
                Attach(newSource);
            }

            if (animationState.View.IsClosed)
            {
                DetachAll();
            }
        }

        try
        {
            if (PresentationSource.FromVisual(animationState.View.VisualElement) is HwndSource initialSource)
            {
                Attach(initialSource);
            }

            PresentationSource.AddSourceChangedHandler(animationState.View.VisualElement, SourceChanged);
            animationState.View.Closed += ViewClosed;
            return DetachAll;
        }
        catch
        {
            DetachAll();
            throw;
        }

        void ViewClosed(object? sender, EventArgs args) => DetachAll();

        void DetachAll()
        {
            Detach();
            PresentationSource.RemoveSourceChangedHandler(animationState.View.VisualElement, SourceChanged);
            animationState.View.Closed -= ViewClosed;
        }
    }

    private static void FrameRendering(object? sender, EventArgs e)
    {
        try
        {
            _activeAnimationStates.RemoveWhere(state => !state.HasPendingUpdates && state.CanQueueAnimation);

            foreach (var animationState in _activeAnimationStates.ToArray())
            {
                if (!animationState.CanQueueAnimation)
                {
                    UnregisterView(animationState);
                    continue;
                }

                try
                {
                    animationState.ApplyPendingAnimations();
                }
                catch (Exception ex)
                {
                    ExtensionDiagnostics.TraceException("FrameRendering.AnimateView", ex);
                }
            }
        }
        catch (Exception ex)
        {
            ExtensionDiagnostics.TraceException("FrameRendering", ex);
        }
        finally
        {
            _activeAnimationStates.RemoveWhere(state => !state.HasPendingUpdates || !state.CanQueueAnimation);
            UpdateRenderingSubscription();
        }
    }

    private static void ActivateAnimation(TextViewAnimationState animationState)
    {
        if (animationState.CanQueueAnimation && animationState.HasPendingUpdates)
        {
            _activeAnimationStates.Add(animationState);
            EnsureRendering();
        }
    }

    private static void EnsureRendering()
    {
        if (IsInitialized && !_isRenderingSubscribed)
        {
            CompositionTarget.Rendering += FrameRendering;
            _isRenderingSubscribed = true;
        }
    }

    private static void StopRendering()
    {
        if (_isRenderingSubscribed)
        {
            CompositionTarget.Rendering -= FrameRendering;
            _isRenderingSubscribed = false;
        }
    }

    private static void UpdateRenderingSubscription()
    {
        if (IsInitialized && _activeAnimationStates.Count > 0)
        {
            EnsureRendering();
        }
        else
        {
            StopRendering();
        }
    }

    public static void HorizontalScroll(TextViewAnimationState animationState, double distance)
    {
        if (animationState.CanQueueAnimation)
        {
            animationState.HorizontalScrollAnimation.Scroll(distance);
            ActivateAnimation(animationState);
        }
    }

    public static void VerticalScroll(TextViewAnimationState animationState, double distance)
    {
        if (animationState.CanQueueAnimation)
        {
            animationState.VerticalScrollAnimation.Scroll(distance);
            ActivateAnimation(animationState);
        }
    }

    public static async Task CleanupAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        IsInitialized = false;
        StopRendering();

        foreach (var view in _pendingViewCleanups.Keys.ToArray())
        {
            RemovePendingView(view);
        }
        foreach (var state in _registeredAnimationStates.Concat(_activeAnimationStates).Distinct().ToArray())
        {
            UnregisterView(state);
        }

        _activeAnimationStates.Clear();
        _registeredAnimationStates.Clear();
        while (_pendingViews.TryDequeue(out _))
        {
        }
    }

    private static double GetVerticalScrollDistance(TextViewAnimationState animationState, int delta, bool isAltPressed)
    {
        var distance = delta * SettingsCache.VerticalScrollRate / 12000.0 * animationState.View.LineHeight * SettingsCache.LinesPerVerticalScroll;

        if (isAltPressed)
        {
            distance *= SettingsCache.FastScrollMultiplier;
        }

        return distance;
    }

    private static double GetHorizontalScrollDistance(TextViewAnimationState animationState, int delta, bool isAltPressed)
    {
        var distance = delta * SettingsCache.HorizontalScrollRate / 12000.0 * animationState.View.FormattedLineSource.ColumnWidth * SettingsCache.CharsPerHorizontalScroll;

        if (isAltPressed)
        {
            distance *= SettingsCache.FastScrollMultiplier;
        }

        return distance;
    }

    private sealed class WindowWheelHook(HwndSource source)
    {
        private readonly Dictionary<DependencyObject, HashSet<TextViewAnimationState>> _statesByElement = [];

        public bool IsEmpty => _statesByElement.Count == 0;

        public void Attach() => source.AddHook(Hook);

        public void Detach()
        {
            if (!source.IsDisposed)
            {
                source.RemoveHook(Hook);
            }
        }

        public void AddView(TextViewAnimationState animationState)
        {
            AddElement(animationState.View.VisualElement, animationState);
            if (animationState.Host?.HostControl is { } hostControl)
            {
                AddElement(hostControl, animationState);
            }
        }

        public void RemoveView(TextViewAnimationState animationState)
        {
            RemoveElement(animationState.View.VisualElement, animationState);
            if (animationState.Host?.HostControl is { } hostControl)
            {
                RemoveElement(hostControl, animationState);
            }
        }

        private void AddElement(DependencyObject element, TextViewAnimationState animationState)
        {
            if (!_statesByElement.TryGetValue(element, out var states))
            {
                states = [];
                _statesByElement.Add(element, states);
            }

            states.Add(animationState);
        }

        private void RemoveElement(DependencyObject element, TextViewAnimationState animationState)
        {
            if (_statesByElement.TryGetValue(element, out var states))
            {
                states.Remove(animationState);
                if (states.Count == 0)
                {
                    _statesByElement.Remove(element);
                }
            }
        }

        private nint Hook(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
        {
            const int WM_MOUSEWHEEL = 0x020A;
            const int WM_MOUSEHWHEEL = 0x020E;
            const int MK_CONTROL = 0x0008;
            const int MK_SHIFT = 0x0004;

            try
            {
                if (msg != WM_MOUSEWHEEL && msg != WM_MOUSEHWHEEL)
                {
                    return default;
                }

                var screenPoint = new Point(unchecked((short)(long)lParam), unchecked((short)((long)lParam >> 16)));
                if (!TryGetAnimationStateAtPoint(screenPoint, out var animationState))
                {
                    return default;
                }

                var delta = (int)wParam >> 16;
                var isAltPressed = Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);
                if (msg == WM_MOUSEWHEEL && (wParam & MK_CONTROL) == MK_CONTROL && animationState.CanZoom)
                {
                    var scale = delta > 0 ? delta / 1200.0 : delta / 1320.0;
                    animationState.ZoomAnimation.Zoom(animationState.View.ZoomLevel, scale, isAltPressed);
                }
                else if (msg == WM_MOUSEHWHEEL)
                {
                    animationState.HorizontalScrollAnimation.Scroll(GetHorizontalScrollDistance(animationState, delta, isAltPressed));
                }
                else if ((wParam & MK_SHIFT) == MK_SHIFT)
                {
                    animationState.HorizontalScrollAnimation.Scroll(-GetHorizontalScrollDistance(animationState, delta, isAltPressed));
                }
                else
                {
                    animationState.VerticalScrollAnimation.Scroll(GetVerticalScrollDistance(animationState, delta, isAltPressed));
                }

                ActivateAnimation(animationState);
                handled = true;
            }
            catch (Exception ex)
            {
                ExtensionDiagnostics.TraceException("HwndSource.Hook", ex);
            }

            return default;
        }

        private bool TryGetAnimationStateAtPoint(Point screenPoint, out TextViewAnimationState animationState)
        {
            animationState = null!;
            if (source is not { IsDisposed: false, RootVisual: UIElement root })
            {
                return false;
            }

            var hit = root.InputHitTest(root.PointFromScreen(screenPoint)) as DependencyObject;
            while (hit is not null)
            {
                if (_statesByElement.TryGetValue(hit, out var states))
                {
                    foreach (var state in states)
                    {
                        if (state.CanQueueAnimation)
                        {
                            animationState = state;
                            return true;
                        }
                    }
                }

                hit = hit is Visual ? VisualTreeHelper.GetParent(hit) : LogicalTreeHelper.GetParent(hit);
            }

            return false;
        }
    }
}
