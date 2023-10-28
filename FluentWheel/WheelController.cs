using System;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Editor;

namespace Cloris.FluentWheel;

internal class WheelController
{
    private readonly IWpfTextView _wpfTextView;
    private readonly ScrollCalculation _horizontalScrollCalculation = new();
    private readonly ScrollCalculation _verticalScrollCalculation = new();
    private readonly ZoomCalculation _zoomCalculation = new();

    public WheelController(IWpfTextView wpfTextView)
    {
        _wpfTextView = wpfTextView;

        _wpfTextView.VisualElement.Loaded += delegate
        {
            CompositionTarget.Rendering += FrameRendering;
        };

        _wpfTextView.VisualElement.Unloaded += delegate
        {
            CompositionTarget.Rendering -= FrameRendering;
        };
    }

    public bool HandleWheel(int delta)
    {
        if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
        {
            if (!Settings.Current.IsZoomingEnabled)
                return false;

            _zoomCalculation.Zoom(_wpfTextView.ZoomLevel, delta / 1200.0);
            return true;
        }

        if (Settings.Current.IsHorizontalScrollingEnabled
            && (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
        {
            _horizontalScrollCalculation.Scroll(delta * Settings.Current.HorizontalScrollRate / -100.0);
            return true;
        }

        _verticalScrollCalculation.Scroll(delta * Settings.Current.VerticalScrollRate / 100.0);
        return true;
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
}