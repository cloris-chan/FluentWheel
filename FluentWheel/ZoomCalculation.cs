using System;
using System.Linq;
using Microsoft.VisualStudio.Utilities;

namespace Cloris.FluentWheel;

internal class ZoomCalculation
{
    private static readonly int[] _fixedZoomLevels = [20, 25, 35, 50, 75, 100, 125, 150, 175, 200, 250, 300, 350, 400];

    private PooledStopwatch _stopwatch;

    private double _initialZoomLevel;
    private double _targetZoomLevel;
    private double _zoomVelocity;

    public bool IsZooming { get; private set; }

    public void Zoom(double currentZoomLevel, double scale, bool useFixedZoomLevels)
    {
        _initialZoomLevel = currentZoomLevel;

        var baseLevel = IsZooming && Math.Sign(_zoomVelocity) == Math.Sign(scale) ? _targetZoomLevel : currentZoomLevel;

        _targetZoomLevel = useFixedZoomLevels
            ? scale > 0 ? _fixedZoomLevels.First(x => x > baseLevel || x == 400) : _fixedZoomLevels.Last(x => x < baseLevel || x == 20)
            : Math.Round(baseLevel + baseLevel * scale) switch
            {
                < 20 => 20,
                > 400 => 400,
                var level => level,
            };

        if (!IsZooming)
        {
            IsZooming = true;
        }

        _zoomVelocity = Math.Pow(_targetZoomLevel / _initialZoomLevel, Settings.Current.ZoomDuration == 0 ? 1.0 : 1.0 / Settings.Current.ZoomDuration);
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