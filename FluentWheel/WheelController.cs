using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Editor;

namespace Cloris.FluentWheel;

internal class WheelController
{
    private const int WM_MOUSEWHEEL = 0x020A;
    private const int MK_CONTROL = 0x0008;
    private const int MK_SHIFT = 0x0004;

    private readonly ScrollCalculation _horizontalScrollCalculation = new();
    private readonly ScrollCalculation _verticalScrollCalculation = new();
    private readonly ZoomCalculation _zoomCalculation = new();

    private readonly IWpfTextView _wpfTextView;
    private HwndSource _hwndSource;
    private double _width;
    private double _height;

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

        _wpfTextView.VisualElement.SizeChanged += (_, e) =>
        {
            _width = e.NewSize.Width;
            _height = e.NewSize.Height;
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
        _hwndSource = (HwndSource)PresentationSource.FromVisual(_wpfTextView.VisualElement);
        _hwndSource.AddHook(Handler);
    }

    private void Unhook()
    {
        _hwndSource.RemoveHook(Handler);
    }

    private nint Handler(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg is not WM_MOUSEWHEEL)
            return default;

        var cursorPosition = _wpfTextView.VisualElement.PointFromScreen(new((int)lParam & 0xFFFF, ((int)lParam >> 16) & 0xFFFF));
        if (cursorPosition.X < 0 || cursorPosition.Y < 0 || cursorPosition.X > _width || cursorPosition.Y > _height)
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
}