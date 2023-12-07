using System;
using System.Runtime.InteropServices;
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
    private double _width;
    private double _height;

    private bool _isHooked;

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
        if (!_isHooked && TryGetHwndSource(out var hwndSource))
        {
            hwndSource.AddHook(Handler);
            _isHooked = true;
        }
    }

    private void Unhook()
    {
        if (_isHooked && TryGetHwndSource(out var hwndSource))
        {
            hwndSource.RemoveHook(Handler);
            _isHooked = false;
        }
    }

    private nint Handler(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg is not WM_MOUSEWHEEL)
            return default;

        if (Settings.Current is null)
            return default;

        if (!IsOnCurrentVisualElement(lParam))
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

    private bool IsOnCurrentVisualElement(nint lParam)
    {
        if (!TryGetHwndSource(out var hwndSource))
            return false;

        var handleRef = new HandleRef(hwndSource, hwndSource.Handle);
        var pt = new NativeMethods.POINT
        {
            X = (int)lParam & 0xFFFF,
            Y = ((int)lParam >> 16) & 0xFFFF
        };
        NativeMethods.ScreenToClient(handleRef, ref pt);

        var point = new Point(pt.X, pt.Y);
        point = hwndSource.CompositionTarget.TransformFromDevice.Transform(point);

        if (hwndSource.RootVisual is null)
            return false;

        Matrix visualTransform = Matrix.Identity;
        if (VisualTreeHelper.GetTransform(hwndSource.RootVisual) is Transform transform)
            visualTransform = Matrix.Multiply(visualTransform, transform.Value);

        Vector offset = VisualTreeHelper.GetOffset(hwndSource.RootVisual);
        visualTransform.Translate(offset.X, offset.Y);
        visualTransform.Invert();
        point = visualTransform.Transform(point);

        if (hwndSource.RootVisual.TransformToDescendant(_wpfTextView.VisualElement) is not GeneralTransform generalTransform)
            return false;

        if (!generalTransform.TryTransform(point, out point))
            return false;

        if (point.X < 0 || point.Y < 0 || point.X > _width || point.Y > _height)
            return false;

        return true;
    }

    private bool TryGetHwndSource(out HwndSource hwndSource)
    {
        hwndSource = PresentationSource.FromVisual(_wpfTextView.VisualElement) as HwndSource;
        return hwndSource is not null;
    }
}