using Microsoft.VisualStudio.Text.Editor;

namespace Cloris.FluentWheel;

internal sealed class TextViewAnimationState(IWpfTextView wpfTextView, ViewScroller viewScroller)
{
    public IWpfTextView View => wpfTextView;

    public ViewScroller ViewScroller => viewScroller;

    public ScrollAnimation HorizontalScrollAnimation { get; } = new();

    public ScrollAnimation VerticalScrollAnimation { get; } = new();

    public ZoomAnimation ZoomAnimation { get; } = new();

    public bool IsAnimating => HorizontalScrollAnimation.IsAnimating || VerticalScrollAnimation.IsAnimating || ZoomAnimation.IsAnimating;
}
