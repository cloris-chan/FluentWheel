using Microsoft.VisualStudio.Text.Editor;

namespace Cloris.FluentWheel;

internal class WheelCalculator(IWpfTextView wpfTextView, ViewScroller viewScroller)
{
    public IWpfTextView View => wpfTextView;

    public ViewScroller ViewScroller => viewScroller;

    public ScrollCalculation HorizontalScrollCalculation { get; } = new();

    public ScrollCalculation VerticalScrollCalculation { get; } = new();

    public ZoomCalculation ZoomCalculation { get; } = new();

    public bool IsRunning => HorizontalScrollCalculation.IsScrolling || VerticalScrollCalculation.IsScrolling || ZoomCalculation.IsZooming;
}