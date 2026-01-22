using Microsoft.VisualStudio.Utilities;

namespace Cloris.FluentWheel;

internal sealed class ScrollAnimation
{
    private PooledStopwatch? _stopwatch;

    private double _totalScrollDistance;
    private double _scrolledDistance;
    private double _scrollSpeed;
    private long _elapsedTime;

    public bool IsAnimating { get; private set; } = false;

    public void Scroll(double distance)
    {
        if (IsAnimating && Math.Sign(_scrollSpeed) == Math.Sign(distance))
        {
            _totalScrollDistance = distance + _totalScrollDistance - _scrolledDistance;
        }
        else
        {
            _totalScrollDistance = distance;
            IsAnimating = true;
        }

        _elapsedTime = 0;
        _scrolledDistance = 0;
        _scrollSpeed = SettingsCache.ScrollDuration == 0 ? _totalScrollDistance : _totalScrollDistance / SettingsCache.ScrollDuration;
        Start();
    }

    public double CalculateDistance()
    {
        var elapsedTime = _stopwatch!.ElapsedMilliseconds;
        double distance;

        if (elapsedTime >= SettingsCache.ScrollDuration)
        {
            distance = _totalScrollDistance - _scrolledDistance;
            Reset();
            return distance;
        }

        distance = _scrollSpeed * (elapsedTime - _elapsedTime);
        _elapsedTime = elapsedTime;
        _scrolledDistance += distance;
        return distance;
    }

    private void Start()
    {
        _stopwatch?.Free();
        _stopwatch = PooledStopwatch.StartInstance();
    }

    private void Reset()
    {
        _stopwatch?.Free();
        _stopwatch = null;
        _totalScrollDistance = 0;
        _scrolledDistance = 0;
        _elapsedTime = 0;
        IsAnimating = false;
    }
}
