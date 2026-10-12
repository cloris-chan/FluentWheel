namespace Cloris.FluentWheel;

internal sealed class ZoomAnimation : AnimationBase
{
    private static readonly int[] _fixedZoomLevels = [20, 25, 35, 50, 75, 100, 125, 150, 175, 200, 250, 300, 350, 400];

    protected override double Duration => SettingsCache.ZoomDuration;

    public void Zoom(double currentZoomLevel, double scale, bool useFixedZoomLevels)
    {
        if (scale == 0)
        {
            return;
        }

        double startValue, targetValue;

        if (HasPendingUpdate)
        {
            var previousTargetValue = _targetValue;
            startValue = SampleValue();
            targetValue = Math.Sign(previousTargetValue - startValue) == Math.Sign(scale)
                ? GetTargetZoomLevel(previousTargetValue, scale, useFixedZoomLevels)
                : GetTargetZoomLevel(startValue, scale, useFixedZoomLevels);
        }
        else
        {
            Reset(currentZoomLevel);
            startValue = currentZoomLevel;
            targetValue = GetTargetZoomLevel(startValue, scale, useFixedZoomLevels);
        }

        BeginAnimation(startValue, targetValue);
    }

    protected override double ToAnimationValue(double value) => Math.Log(value);

    protected override double FromAnimationValue(double value) => Math.Round(Math.Exp(value), 5);

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
