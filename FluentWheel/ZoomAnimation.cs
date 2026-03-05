using Microsoft.VisualStudio.Utilities;

namespace Cloris.FluentWheel;

internal sealed class ZoomAnimation
{
    private static readonly int[] _fixedZoomLevels = [20, 25, 35, 50, 75, 100, 125, 150, 175, 200, 250, 300, 350, 400];

    private PooledStopwatch? _stopwatch;

    private double _initialZoomLevel;
    private double _targetZoomLevel;
    private double _zoomVelocity;

    public bool IsAnimating { get; private set; }

    public void Zoom(double currentZoomLevel, double scale, bool useFixedZoomLevels)
    {
        _initialZoomLevel = currentZoomLevel;

        var baseLevel = IsAnimating && Math.Sign(_targetZoomLevel - _initialZoomLevel) == Math.Sign(scale) ? _targetZoomLevel : currentZoomLevel;

        _targetZoomLevel = useFixedZoomLevels
            ? scale > 0 ? _fixedZoomLevels.First(x => x > baseLevel || x == 400) : _fixedZoomLevels.Last(x => x < baseLevel || x == 20)
            : Math.Round(baseLevel + baseLevel * scale) switch
            {
                < 20 => 20,
                > 400 => 400,
                var level => level,
            };

        if (!IsAnimating)
        {
            IsAnimating = true;
        }

        _zoomVelocity = Math.Pow(_targetZoomLevel / _initialZoomLevel, SettingsCache.ZoomDuration == 0 ? 1.0 : 1.0 / SettingsCache.ZoomDuration);
        Start();
    }

    public double CalculateZoom()
    {
        var elsapsedTime = _stopwatch!.ElapsedMilliseconds;
        double zoomLevel;

        if (elsapsedTime >= SettingsCache.ZoomDuration)
        {
            zoomLevel = _targetZoomLevel;
            Reset();
            return zoomLevel;
        }

        zoomLevel = _initialZoomLevel * Math.Pow(_zoomVelocity, elsapsedTime);
        return Math.Round(zoomLevel, 5);
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
        _initialZoomLevel = 0;
        _targetZoomLevel = 0;
        _zoomVelocity = 0;
        IsAnimating = false;
    }
}
