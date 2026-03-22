using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Text.Editor;
using System.Windows;
using System.Windows.Controls;

namespace Cloris.FluentWheel;

internal sealed class TextViewAnimationState
{
    public IWpfTextView View { get; }

    public ViewScroller ViewScroller { get; }

    public IWpfTextViewHost? Host { get; private set; }

    public ScrollAnimation HorizontalScrollAnimation { get; } = new();

    public ScrollAnimation VerticalScrollAnimation { get; } = new();

    public ZoomAnimation ZoomAnimation { get; } = new();

    public bool IsAnimating => HorizontalScrollAnimation.IsAnimating || VerticalScrollAnimation.IsAnimating || ZoomAnimation.IsAnimating;

    public bool CanAnimate => View is { IsClosed: false, InLayout: false };

    public bool CanZoom { get; private set; }

    public TextViewAnimationState(IWpfTextView wpfTextView)
    {
        const string ZoomControlMarginFullName = "Microsoft.VisualStudio.Text.Editor.Implementation.ZoomControlMargin";

        View = wpfTextView;
        ViewScroller = new ViewScroller(this);
        Host = FindHost(View.VisualElement);
        CanZoom = Host?.HostControl.FindDescendants<ComboBox>().OfType<IWpfTextViewMargin>().Any(item => item.GetType().FullName is ZoomControlMarginFullName) is true;
    }

    private static IWpfTextViewHost? FindHost(DependencyObject element) => element switch
    {
        IWpfTextViewHost host => host,
        FrameworkElement fe => FindHost(fe.Parent),
        _ => null
    };
}
