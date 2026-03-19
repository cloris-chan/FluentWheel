namespace Cloris.FluentWheel;

internal sealed class ZoomAnimation : AnimationBase
{
    private static readonly int[] _fixedZoomLevels = [20, 25, 35, 50, 75, 100, 125, 150, 175, 200, 250, 300, 350, 400];

    protected override double Duration => SettingsCache.ZoomDuration;

    protected override EasingMode EasingMode => SettingsCache.ZoomEasingMode;

    protected override double Tolerance => 0.0001;

    public void Zoom(double currentZoomLevel, double scale, bool useFixedZoomLevels)
    {
        double startValue, targetValue;

        if (IsAnimating)
        {
            var previousTargetValue = _targetValue;
            startValue = GetCurrentValue().Next;
            targetValue = Math.Sign(previousTargetValue - startValue) == Math.Sign(scale)
                ? GetTargetZoomLevel(previousTargetValue, scale, useFixedZoomLevels)
                : GetTargetZoomLevel(startValue, scale, useFixedZoomLevels);
        }
        else
        {
            startValue = currentZoomLevel;
            targetValue = GetTargetZoomLevel(startValue, scale, useFixedZoomLevels);
        }

        BeginAnimation(startValue, targetValue);
    }

    public double CalculateZoomLevel()
    {
        if (!IsAnimating)
        {
            return _targetValue;
        }

        return GetCurrentValue().Next;
    }

    protected override double InterpolateValue(double progress)
    {
        return Math.Round(_startValue * Math.Pow(_targetValue / _startValue, progress), 5);
    }

    private static double GetTargetZoomLevel(double baseZoomLevel, double scale, bool useFixedZoomLevels)
    {
        if (useFixedZoomLevels)
        {
            return scale > 0 ? _fixedZoomLevels.First(x => x > baseZoomLevel || x == 400) : _fixedZoomLevels.Last(x => x < baseZoomLevel || x == 20);
        }
        else
        {
            return Math.Round(baseZoomLevel + (baseZoomLevel * scale)) switch
            {
                < 20 => 20,
                > 400 => 400,
                var level => level,
            };
        }
    }
}
