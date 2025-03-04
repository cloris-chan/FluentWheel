using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;

namespace Cloris.FluentWheel;

internal static class WheelController
{
    private const int WM_MOUSEWHEEL = 0x020A;
    private const int MK_CONTROL = 0x0008;

    private static readonly ConcurrentQueue<IWpfTextView> _pendingViews = [];
    private static readonly ConditionalWeakTable<IWpfTextView, WheelCalculator> _calculators = new();
    private static readonly HashSet<nint> _hookedHandles = [];
    private static readonly HashSet<WheelCalculator> _runningCalculators = [];
    private static IWpfTextView _currentView;

    public static async ValueTask InitializeAsync(AsyncPackage package)
    {
        await package.JoinableTaskFactory.SwitchToMainThreadAsync();

        while (_pendingViews.TryDequeue(out var view))
        {
            RegisterInternal(view);
        }

        CompositionTarget.Rendering += FrameRendering;
    }

    public static void Register(IWpfTextView view)
    {
        if (Settings.IsInitialized)
        {
            RegisterInternal(view);
        }
        else
        {
            _pendingViews.Enqueue(view);
        }
    }

    private static void RegisterInternal(IWpfTextView view)
    {
        if (view.VisualElement.IsInitialized)
        {
            Initialization(view);
        }
        else
        {
            view.VisualElement.Initialized += delegate
            {
                Initialization(view);
            };
        }
    }

    private static void Initialization(IWpfTextView view)
    {
        var viewScroller = new ViewScroller(view);

        var innerViewScrollField = view.GetType().GetField("_viewScroller", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (innerViewScrollField is not null && typeof(IViewScroller).IsAssignableFrom(innerViewScrollField.FieldType))
        {
            innerViewScrollField.SetValue(view, viewScroller);
            _calculators.Add(view, new WheelCalculator(view, viewScroller));

            if (view.VisualElement.IsLoaded)
            {
                Load(view);
            }
            else
            {
                view.VisualElement.Loaded += delegate
                {
                    Load(view);
                };
            }

            view.VisualElement.MouseEnter += delegate
            {
                _currentView = view;
            };
            view.VisualElement.MouseLeave += delegate
            {
                if (_currentView == view)
                {
                    _currentView = null;
                }
            };

            if (view.IsMouseOverViewOrAdornments)
            {
                _currentView = view;
            }
        }
    }

    private static void Load(IWpfTextView view)
    {
        if (TryGetHwndSource(view, out var hwndSource))
        {
            var handle = hwndSource.Handle;
            if (hwndSource.IsDisposed || _hookedHandles.Contains(handle))
                return;

            hwndSource.AddHook(Handler);
            _hookedHandles.Add(handle);
            hwndSource.Disposed += delegate
            {
                _hookedHandles.Remove(handle);
            };
        }
    }

    private static void FrameRendering(object sender, EventArgs e)
    {
        foreach (var calculator in _runningCalculators)
        {
            if (calculator.View?.IsClosed is not false)
            {
                continue;
            }

            if (calculator.VerticalScrollCalculation.IsScrolling)
            {
                var distance = calculator.VerticalScrollCalculation.CalculateDistance();
                calculator.ViewScroller.VerticallyScroll(distance);
            }

            if (calculator.HorizontalScrollCalculation.IsScrolling)
            {
                var distance = calculator.HorizontalScrollCalculation.CalculateDistance();
                calculator.ViewScroller.HorizontallyScroll(distance);
            }

            if (calculator.ZoomCalculation.IsZooming)
            {
                var zoomLevel = calculator.ZoomCalculation.CalculateZoom();
                calculator.View.ZoomLevel = zoomLevel;
            }
        }

        _runningCalculators.RemoveWhere(calculator => !calculator.IsRunning || calculator.View?.IsClosed is not false);
    }

    public static void HandleHorizontallyScroll(IWpfTextView view, double distance)
    {
        if (_calculators.TryGetValue(view, out var calculator) && calculator.View is not null && !calculator.View.IsClosed)
        {
            calculator.HorizontalScrollCalculation.Scroll(distance);
            _runningCalculators.Add(calculator);
        }
    }

    public static void HandleVerticallyScroll(IWpfTextView view, double distance)
    {
        if (view?.IsClosed is false && _calculators.TryGetValue(view, out var calculator))
        {
            calculator.VerticalScrollCalculation.Scroll(distance);
            _runningCalculators.Add(calculator);
        }
    }

    private static nint Handler(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg is not WM_MOUSEWHEEL)
            return default;

        if (Settings.Current is null)
            return default;

        if (_currentView is null || !_calculators.TryGetValue(_currentView, out var calculator) || calculator.View is null || calculator.View.IsClosed)
            return default;

        var delta = (int)wParam >> 16;

        if ((wParam & MK_CONTROL) is MK_CONTROL)
        {
            if (Settings.Current.IsZoomingEnabled && calculator.View is not null && !calculator.View.IsClosed)
            {
                calculator.ZoomCalculation.Zoom(calculator.View.ZoomLevel, delta / 1200.0);
                _runningCalculators.Add(calculator);
                handled = true;
            }
            return default;
        }

        return default;
    }

    private static bool TryGetHwndSource(IWpfTextView view, out HwndSource hwndSource)
    {
        hwndSource = PresentationSource.FromVisual(view.VisualElement) as HwndSource;
        return hwndSource is not null;
    }
}