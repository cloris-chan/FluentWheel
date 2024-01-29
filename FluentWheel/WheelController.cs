using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Editor;

namespace Cloris.FluentWheel;

internal class WheelController
{
    private const int WM_MOUSEWHEEL = 0x020A;
    private const int MK_CONTROL = 0x0008;
    private const int MK_SHIFT = 0x0004;

    private readonly IWpfTextView _wpfTextView;

    private readonly ScrollCalculation _horizontalScrollCalculation = new();
    private readonly ScrollCalculation _verticalScrollCalculation = new();
    private readonly ZoomCalculation _zoomCalculation = new();

    private bool _isHooked;
    private bool _isOnCurrentVisual;

    public WheelController(IWpfTextView wpfTextView)
    {
        _wpfTextView = wpfTextView;

        _wpfTextView.VisualElement.Loaded += delegate
        {
            Hook();
            CompositionTarget.Rendering += FrameRendering;
        };

        _wpfTextView.VisualElement.Unloaded += delegate
        {
            CompositionTarget.Rendering -= FrameRendering;
            Unhook();
        };
    }

    private void FrameRendering(object sender, EventArgs e)
    {
        if (_verticalScrollCalculation.IsScrolling)
        {
            var distance = _verticalScrollCalculation.CalculateDistance();
            _wpfTextView.ViewScroller.ScrollViewportVerticallyByPixels(distance);
        }

        if (_horizontalScrollCalculation.IsScrolling)
        {
            var distance = _horizontalScrollCalculation.CalculateDistance();
            _wpfTextView.ViewScroller.ScrollViewportHorizontallyByPixels(distance);
        }

        if (_zoomCalculation.IsZooming)
        {
            var zoomLevel = _zoomCalculation.CalculateZoom();
            _wpfTextView.ZoomLevel = zoomLevel;
        }
    }

    private void Hook()
    {
        if (!_isHooked && TryGetHwndSource(out var hwndSource))
        {
            hwndSource.AddHook(Handler);
            _wpfTextView.VisualElement.MouseEnter += MouseEnter;
            _wpfTextView.VisualElement.MouseLeave += MouseLeave;
            _isHooked = true;
        }
    }

    private void Unhook()
    {
        if (_isHooked && TryGetHwndSource(out var hwndSource))
        {
            hwndSource.RemoveHook(Handler);
            _wpfTextView.VisualElement.MouseEnter -= MouseEnter;
            _wpfTextView.VisualElement.MouseLeave -= MouseLeave;
            _isHooked = false;
        }
    }

    private void MouseEnter(object sender, MouseEventArgs e)
    {
        _isOnCurrentVisual = true;
    }

    private void MouseLeave(object sender, MouseEventArgs e)
    {
        _isOnCurrentVisual = false;
    }

    private nint Handler(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg is not WM_MOUSEWHEEL)
            return default;

        if (Settings.Current is null)
            return default;

        if (!_isOnCurrentVisual)
            return default;

        var delta = (int)wParam >> 16;

        if ((wParam & MK_CONTROL) is MK_CONTROL)
        {
            if (Settings.Current.IsZoomingEnabled)
            {
                _zoomCalculation.Zoom(_wpfTextView.ZoomLevel, delta / 1200.0);
                handled = true;
            }
            return default;
        }

        if ((wParam & MK_SHIFT) is MK_SHIFT)
        {
            if (Settings.Current.IsHorizontalScrollingEnabled)
            {
                _horizontalScrollCalculation.Scroll(delta * Settings.Current.HorizontalScrollRate / -100.0);
                handled = true;
            }
            return default;
        }

        _verticalScrollCalculation.Scroll(delta * Settings.Current.VerticalScrollRate / 100.0);
        handled = true;
        return default;
    }

    private bool TryGetHwndSource(out HwndSource hwndSource)
    {
        hwndSource = PresentationSource.FromVisual(_wpfTextView.VisualElement) as HwndSource;
        return hwndSource is not null;
    }
}