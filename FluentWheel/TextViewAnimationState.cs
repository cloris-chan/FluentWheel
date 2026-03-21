using Microsoft.VisualStudio.Text.Editor;
using System.Windows;

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

    public bool CanAnimate => View is { IsClosed: false, InLayout: false };

    public IWpfTextViewHost? Host
    {
        get
        {
            if (field is null)
            {
                for (var element = View.VisualElement; element is not null; element = element.Parent as FrameworkElement)
                {
                    if (element.Parent is IWpfTextViewHost host)
                    {
                        field = host;
                        break;
                    }
                }
            }

            return field;
        }
    }
}
