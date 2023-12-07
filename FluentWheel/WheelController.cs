using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;

namespace Cloris.FluentWheel;

internal static class WheelController
{
    private const int WM_MOUSEWHEEL = 0x020A;
    private const int MK_CONTROL = 0x0008;
    private const int MK_SHIFT = 0x0004;

    private static readonly ConditionalWeakTable<IWpfTextView, WheelCalculator> _calculators = new();
    private static readonly HashSet<nint> _hookedHandles = [];
    private static readonly HashSet<WheelCalculator> _runningCalculators = [];
    private static IWpfTextView _currentView;

    public static async ValueTask InitializeAsync(AsyncPackage package)
    {
        await package.JoinableTaskFactory.SwitchToMainThreadAsync();
        CompositionTarget.Rendering += FrameRendering;
    }

    public static void Register(IWpfTextView view)
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
    }

    private static void Initialization(IWpfTextView view)
    {
        void MouseEnter(object sender, MouseEventArgs e)
        {
            _currentView = view;
        }

        void MouseLeave(object sender, MouseEventArgs e)
        {
            if (_currentView == view)
                _currentView = null;
        }

        view.VisualElement.MouseEnter += MouseEnter;
        view.VisualElement.MouseLeave += MouseLeave;

        _calculators.Add(view, new WheelCalculator(view));
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
        if (_runningCalculators.Count == 0)
            return;

        foreach (var calculator in _runningCalculators)
        {
            if (calculator.VerticalScrollCalculation.IsScrolling)
            {
                var distance = calculator.VerticalScrollCalculation.CalculateDistance();
                calculator.View.ViewScroller.ScrollViewportVerticallyByPixels(distance);
            }

            if (calculator.HorizontalScrollCalculation.IsScrolling)
            {
                var distance = calculator.HorizontalScrollCalculation.CalculateDistance();
                calculator.View.ViewScroller.ScrollViewportHorizontallyByPixels(distance);
            }

            if (calculator.ZoomCalculation.IsZooming)
            {
                var zoomLevel = calculator.ZoomCalculation.CalculateZoom();
                calculator.View.ZoomLevel = zoomLevel;
            }
        }

        _runningCalculators.RemoveWhere(calculator => !calculator.IsRunning);
    }

    private static nint Handler(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg is not WM_MOUSEWHEEL)
            return default;

        if (Settings.Current is null)
            return default;

        if (_currentView is null || !_calculators.TryGetValue(_currentView, out var calculator))
            return default;

        var delta = (int)wParam >> 16;

        if ((wParam & MK_CONTROL) is MK_CONTROL)
        {
            if (Settings.Current.IsZoomingEnabled)
            {
                calculator.ZoomCalculation.Zoom(calculator.View.ZoomLevel, delta / 1200.0);
                _runningCalculators.Add(calculator);
                handled = true;
            }
            return default;
        }

        if ((wParam & MK_SHIFT) is MK_SHIFT)
        {
            if (Settings.Current.IsHorizontalScrollingEnabled)
            {
                calculator.HorizontalScrollCalculation.Scroll(delta * Settings.Current.HorizontalScrollRate / -100.0);
                _runningCalculators.Add(calculator);
                handled = true;
            }
            return default;
        }

        calculator.VerticalScrollCalculation.Scroll(delta * Settings.Current.VerticalScrollRate / 100.0);
        _runningCalculators.Add(calculator);
        handled = true;
        return default;
    }

    private static bool TryGetHwndSource(IWpfTextView view, out HwndSource hwndSource)
    {
        hwndSource = PresentationSource.FromVisual(view.VisualElement) as HwndSource;
        return hwndSource is not null;
    }
}