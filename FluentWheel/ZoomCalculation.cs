using System;
using Microsoft.VisualStudio.Utilities;

namespace Cloris.FluentWheel;

internal class ZoomCalculation
{
    private PooledStopwatch _stopwatch;

    private double _initialZoomLevel;
    private double _targetZoomLevel;
    private double _zoomVelocity;

    public bool IsZooming { get; private set; }

    public void Zoom(double currentZoomLevel, double scale)
    {
        _initialZoomLevel = currentZoomLevel;

        if (IsZooming && Math.Sign(_zoomVelocity) == Math.Sign(scale))
        {
            _targetZoomLevel += Math.Truncate(_targetZoomLevel * scale);
        }
        else
        {
            _targetZoomLevel = Math.Truncate(currentZoomLevel + currentZoomLevel * scale);
            IsZooming = true;
        }

        if (_targetZoomLevel > 400)
            _targetZoomLevel = 400;
        else if (_targetZoomLevel < 20)
            _targetZoomLevel = 20;

        _zoomVelocity = Math.Pow(_targetZoomLevel / _initialZoomLevel, 1.0 / Settings.Current.ZoomDuration);
        Start();
    }

    public double CalculateZoom()
    {
        var elsapsedTime = _stopwatch.ElapsedMilliseconds;
        double zoomLevel;

        if (elsapsedTime >= Settings.Current.ZoomDuration)
        {
            zoomLevel = _targetZoomLevel;
            Reset();
            return zoomLevel;
        }

        zoomLevel = _initialZoomLevel * Math.Pow(_zoomVelocity, elsapsedTime);
        return zoomLevel;
    }

    private void Start()
    {
        _stopwatch?.Free();
        _stopwatch = PooledStopwatch.StartInstance();
    }

    private void Reset()
    {
        _stopwatch.Free();
        _stopwatch = null;
        _initialZoomLevel = 0;
        _targetZoomLevel = 0;
        _zoomVelocity = 0;
        IsZooming = false;
    }
}