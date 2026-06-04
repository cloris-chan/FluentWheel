using System.Windows.Input;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Cloris.FluentWheel;

internal sealed class ViewScroller(TextViewAnimationState animationState) : IViewScroller
{
    private readonly IViewScroller _innerViewScroller = animationState.View.ViewScroller;

    public void HorizontallyScroll(double distance)
    {
        animationState.View.ViewportLeft += distance;
    }

    public void VerticallyScroll(double distance)
    {
        _innerViewScroller.ScrollViewportVerticallyByPixels(distance);
    }

    public void EnsureSpanVisible(SnapshotSpan span)
    {
        _innerViewScroller.EnsureSpanVisible(span);
    }

    public void EnsureSpanVisible(SnapshotSpan span, EnsureSpanVisibleOptions options)
    {
        _innerViewScroller.EnsureSpanVisible(span, options);
    }

    public void EnsureSpanVisible(VirtualSnapshotSpan span, EnsureSpanVisibleOptions options)
    {
        _innerViewScroller.EnsureSpanVisible(span, options);
    }

    public void ScrollViewportHorizontallyByPixels(double distanceToScroll)
    {
        WheelEngine.HorizontalScroll(animationState, distanceToScroll * SettingsCache.HorizontalScrollRate / 100.0);
    }

    public void ScrollViewportVerticallyByPixels(double distanceToScroll)
    {
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        {
            WheelEngine.HorizontalScroll(animationState, distanceToScroll * SettingsCache.HorizontalScrollRate / -100.0);
        }
        else
        {
            WheelEngine.VerticalScroll(animationState, distanceToScroll * SettingsCache.VerticalScrollRate / 100.0);
        }
    }

    public void ScrollViewportVerticallyByLine(ScrollDirection direction)
    {
        switch (direction)
        {
            case ScrollDirection.Up:
                WheelEngine.VerticalScroll(animationState, animationState.View.LineHeight);
                break;
            case ScrollDirection.Down:
                WheelEngine.VerticalScroll(animationState, -animationState.View.LineHeight);
                break;
            default:
                _innerViewScroller.ScrollViewportVerticallyByLine(direction);
                break;
        }
    }

    public void ScrollViewportVerticallyByLines(ScrollDirection direction, int count)
    {
        switch (direction)
        {
            case ScrollDirection.Up:
                WheelEngine.VerticalScroll(animationState, animationState.View.LineHeight * count);
                break;
            case ScrollDirection.Down:
                WheelEngine.VerticalScroll(animationState, -animationState.View.LineHeight * count);
                break;
            default:
                _innerViewScroller.ScrollViewportVerticallyByLines(direction, count);
                break;
        }
    }

    public bool ScrollViewportVerticallyByPage(ScrollDirection direction)
    {
        return _innerViewScroller.ScrollViewportVerticallyByPage(direction);
    }
}
