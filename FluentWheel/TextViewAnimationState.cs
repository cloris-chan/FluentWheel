using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Text.Editor;

namespace Cloris.FluentWheel;

internal sealed class TextViewAnimationState
{
    private readonly Func<bool> _isInOuterLayout;

    public IWpfTextView View { get; }

    public ViewScroller ViewScroller { get; }

    public IWpfTextViewHost? Host { get; private set; }

    public ScrollAnimation HorizontalScrollAnimation { get; } = new();

    public ScrollAnimation VerticalScrollAnimation { get; } = new();

    public ZoomAnimation ZoomAnimation { get; } = new();

    public bool IsAnimating => HorizontalScrollAnimation.IsAnimating || VerticalScrollAnimation.IsAnimating || ZoomAnimation.IsAnimating;

    public bool CanAnimate => View is { IsClosed: false, InLayout: false } && !_isInOuterLayout();

    public bool CanQueueAnimation => !View.IsClosed;

    public bool CanZoom { get; private set; }


    public TextViewAnimationState(IWpfTextView wpfTextView)
    {
        const string ZoomControlMarginFullName = "Microsoft.VisualStudio.Text.Editor.Implementation.ZoomControlMargin";

        _isInOuterLayout = CreateIsInOuterLayoutAccessor(wpfTextView);
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

    private static Func<bool> CreateIsInOuterLayoutAccessor(IWpfTextView view)
    {
        const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        var property = view.GetType().GetProperty("InOuterLayout", Flags);
        if (property?.PropertyType == typeof(bool))
        {
            return () => property.GetValue(view) is true;
        }

        var field = view.GetType().GetField("_inOuterLayout", Flags);
        if (field?.FieldType == typeof(bool))
        {
            return () => field.GetValue(view) is true;
        }

        return static () => false;
    }
}
