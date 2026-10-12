namespace Cloris.FluentWheel;

internal sealed class ScrollAnimation : AnimationBase
{
    protected override double Duration => SettingsCache.ScrollDuration;

    public void Scroll(double distance)
    {
        if (distance == 0)
        {
            return;
        }

        double startValue, targetValue;

        if (HasPendingUpdate)
        {
            var previousTargetValue = _targetValue;
            startValue = SampleValue();
            targetValue = Math.Sign(previousTargetValue - startValue) == Math.Sign(distance)
                ? previousTargetValue + distance
                : startValue + distance;
        }
        else
        {
            Reset();
            startValue = 0;
            targetValue = distance;
        }

        BeginAnimation(startValue, targetValue);
    }
}
