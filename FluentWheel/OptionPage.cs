using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace Cloris.FluentWheel;

internal class OptionPage : DialogPage
{
    [Category("Scroll")]
    [DisplayName("Scroll Duration (ms)")]
    [Description("The duration of the scroll animation in milliseconds.")]
    public int ScrollDuration
    {
        get => Settings.Current.ScrollDuration;
        set => Settings.Current.ScrollDuration = value;
    }

    [Category("Scroll")]
    [DisplayName("Vertical Scroll Rate (%)")]
    [Description("The rate of the vertical scroll animation.")]
    public int VerticalScrollRate
    {
        get => Settings.Current.VerticalScrollRate;
        set => Settings.Current.VerticalScrollRate = value;
    }

    [Category("Scroll")]
    [DisplayName("Is Horizontal Scrolling Enabled")]
    [Description("Enable horizontal scrolling when Shift key is pressed.")]
    public bool IsHorizontalScrollingEnabled
    {
        get => Settings.Current.IsHorizontalScrollingEnabled;
        set => Settings.Current.IsHorizontalScrollingEnabled = value;
    }

    [Category("Scroll")]
    [DisplayName("Horizontal Scroll Rate (%)")]
    [Description("The rate of the horizontal scroll animation.")]
    public int HorizontalScrollRate
    {
        get => Settings.Current.HorizontalScrollRate;
        set => Settings.Current.HorizontalScrollRate = value;
    }

    [Category("Zooming")]
    [DisplayName("Is Zooming Enabled")]
    [Description("Enable zooming when Ctrl key is pressed.")]
    public bool IsZoomingEnabled
    {
        get => Settings.Current.IsZoomingEnabled;
        set => Settings.Current.IsZoomingEnabled = value;
    }

    [Category("Zooming")]
    [DisplayName("Zoom Duration (ms)")]
    [Description("The duration of the zoom animation in milliseconds.")]
    public int ZoomDuration
    {
        get => Settings.Current.ZoomDuration;
        set => Settings.Current.ZoomDuration = value;
    }
}