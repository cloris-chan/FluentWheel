using System;
using System.Diagnostics;

namespace Cloris.FluentWheel;

internal class ScrollCalculation
{
    private readonly Stopwatch _stopwatch = new();

    public double _totalScrollDistance;
    private double _scrolledDistance;
    private double _scrollSpeed;
    private long _elapsedTime;

    public bool IsScrolling { get; private set; } = false;

    public void Scroll(double distance)
    {
        if (IsScrolling && Math.Sign(_scrollSpeed) == Math.Sign(distance))
        {
            _totalScrollDistance = distance + _totalScrollDistance - _scrolledDistance;
            _scrolledDistance = 0;
        }
        else
        {
            _totalScrollDistance = distance;
            IsScrolling = true;
        }

        _elapsedTime = 0;
        _scrollSpeed = _totalScrollDistance / Settings.Current.ScrollDuration;
        _stopwatch.Restart();
    }

    public double CalculateDistance()
    {
        var elapsedTime = _stopwatch.ElapsedMilliseconds;
        double distance;

        if (elapsedTime >= Settings.Current.ScrollDuration)
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

    private void Reset()
    {
        _totalScrollDistance = 0;
        _scrolledDistance = 0;
        _elapsedTime = 0;
        _stopwatch.Reset();
        IsScrolling = false;
    }
}