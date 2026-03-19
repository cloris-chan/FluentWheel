using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System.Windows;
using System.Windows.Input;

namespace Cloris.FluentWheel;

internal sealed class ViewScroller(TextViewAnimationState animationState) : IViewScroller
{
    private const double DefaultWheelScrollLines = 3.0;

    private readonly IViewScroller _innerViewScroller = animationState.View.ViewScroller;

    private static double WheelScrollFactor => SystemParameters.WheelScrollLines > 0 ? SystemParameters.WheelScrollLines / DefaultWheelScrollLines : 1.0;

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
            WheelEngine.HorizontalScroll(animationState, distanceToScroll * WheelScrollFactor * SettingsCache.HorizontalScrollRate / -100.0);
        }
        else
        {
            WheelEngine.VerticalScroll(animationState, distanceToScroll * WheelScrollFactor * SettingsCache.VerticalScrollRate / 100.0);
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
        _innerViewScroller.ScrollViewportVerticallyByLines(direction, count);
    }

    public bool ScrollViewportVerticallyByPage(ScrollDirection direction)
    {
        return _innerViewScroller.ScrollViewportVerticallyByPage(direction);
    }
}
