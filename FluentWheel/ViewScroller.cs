using System.Windows.Input;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Cloris.FluentWheel;

internal class ViewScroller(IWpfTextView view) : IViewScroller
{
    private readonly IViewScroller viewScroller = view.ViewScroller;

    public void HorizontallyScroll(double distance)
    {
        view.ViewportLeft += distance;
    }

    public void VerticallyScroll(double distance)
    {
        viewScroller.ScrollViewportVerticallyByPixels(distance);
    }

    public void EnsureSpanVisible(SnapshotSpan span)
    {
        viewScroller.EnsureSpanVisible(span);
    }

    public void EnsureSpanVisible(SnapshotSpan span, EnsureSpanVisibleOptions options)
    {
        viewScroller.EnsureSpanVisible(span, options);
    }

    public void EnsureSpanVisible(VirtualSnapshotSpan span, EnsureSpanVisibleOptions options)
    {
        viewScroller.EnsureSpanVisible(span, options);
    }

    public void ScrollViewportHorizontallyByPixels(double distanceToScroll)
    {
        WheelController.HandleHorizontallyScroll(view, distanceToScroll * Settings.Current.HorizontalScrollRate / 100.0);
    }

    public void ScrollViewportVerticallyByPixels(double distanceToScroll)
    {
        if (Settings.Current.IsHorizontalScrollingEnabled && (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
        {
            WheelController.HandleHorizontallyScroll(view, distanceToScroll * Settings.Current.HorizontalScrollRate / -100.0);
        }
        else
        {
            WheelController.HandleVerticallyScroll(view, distanceToScroll * Settings.Current.VerticalScrollRate / 100.0);
        }
    }

    public void ScrollViewportVerticallyByLine(ScrollDirection direction)
    {
        switch (direction)
        {
            case ScrollDirection.Up:
                WheelController.HandleVerticallyScroll(view, view.LineHeight);
                break;
            case ScrollDirection.Down:
                WheelController.HandleVerticallyScroll(view, -view.LineHeight);
                break;
            default:
                viewScroller.ScrollViewportVerticallyByLine(direction);
                break;
        }
    }

    public void ScrollViewportVerticallyByLines(ScrollDirection direction, int count)
    {
        switch (direction)
        {
            case ScrollDirection.Up:
                WheelController.HandleVerticallyScroll(view, view.LineHeight * count);
                break;
            case ScrollDirection.Down:
                WheelController.HandleVerticallyScroll(view, -view.LineHeight * count);
                break;
            default:
                viewScroller.ScrollViewportVerticallyByLines(direction, count);
                break;
        }
    }

    public bool ScrollViewportVerticallyByPage(ScrollDirection direction)
    {
        return viewScroller.ScrollViewportVerticallyByPage(direction);
    }
}