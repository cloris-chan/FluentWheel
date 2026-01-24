using Microsoft.VisualStudio.Text.Editor;

namespace Cloris.FluentWheel;

internal sealed class TextViewAnimationState
{
    public TextViewAnimationState(IWpfTextView wpfTextView)
    {
        View = wpfTextView;
        ViewScroller = new ViewScroller(this);
    }

    public IWpfTextView View { get; }

    public ViewScroller ViewScroller { get; }

    public ScrollAnimation HorizontalScrollAnimation { get; } = new();

    public ScrollAnimation VerticalScrollAnimation { get; } = new();

    public ZoomAnimation ZoomAnimation { get; } = new();

    public bool IsAnimating => HorizontalScrollAnimation.IsAnimating || VerticalScrollAnimation.IsAnimating || ZoomAnimation.IsAnimating;
}
