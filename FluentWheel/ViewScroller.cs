using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Cloris.FluentWheel;

internal sealed class ViewScroller(TextViewAnimationState animationState) : IViewScroller
{
    private readonly IViewScroller _innerViewScroller = animationState.ViewportAdapter.NativeScroller;

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
        if (double.IsNaN(distanceToScroll))
        {
            throw new ArgumentOutOfRangeException(nameof(distanceToScroll));
        }

        if (animationState.CanQueueAnimation)
        {
            WheelEngine.HorizontalScroll(animationState, distanceToScroll);
        }
        else
        {
            _innerViewScroller.ScrollViewportHorizontallyByPixels(distanceToScroll);
        }
    }

    public void ScrollViewportVerticallyByPixels(double distanceToScroll)
    {
        if (double.IsNaN(distanceToScroll))
        {
            throw new ArgumentOutOfRangeException(nameof(distanceToScroll));
        }

        if (animationState.CanQueueAnimation)
        {
            WheelEngine.VerticalScroll(animationState, distanceToScroll);
        }
        else
        {
            _innerViewScroller.ScrollViewportVerticallyByPixels(distanceToScroll);
        }
    }

    public void ScrollViewportVerticallyByLine(ScrollDirection direction)
    {
        if (!animationState.CanQueueAnimation)
        {
            _innerViewScroller.ScrollViewportVerticallyByLine(direction);
            return;
        }

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
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        if (!animationState.CanQueueAnimation)
        {
            _innerViewScroller.ScrollViewportVerticallyByLines(direction, count);
            return;
        }

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
