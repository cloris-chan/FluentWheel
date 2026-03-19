namespace Cloris.FluentWheel;

internal sealed class ScrollAnimation : AnimationBase
{
    protected override double Duration => SettingsCache.ScrollDuration;

    protected override EasingMode EasingMode => SettingsCache.ScrollEasingMode;

    protected override double Tolerance => 0.0001;

    public void Scroll(double distance)
    {
        double startValue, targetValue;

        if (IsAnimating)
        {
            var previousTargetValue = _targetValue;
            startValue = GetCurrentValue().Next;
            targetValue = Math.Sign(previousTargetValue - startValue) == Math.Sign(distance)
                ? previousTargetValue + distance
                : startValue + distance;
        }
        else
        {
            startValue = 0;
            targetValue = distance;
        }

        BeginAnimation(startValue, targetValue);
    }

    public double CalculateDistance()
    {
        if (!IsAnimating)
        {
            return 0;
        }

        var (previous, next) = GetCurrentValue();

        return next - previous;
    }

    protected override double InterpolateValue(double progress)
    {
        return _startValue + ((_targetValue - _startValue) * progress);
    }
}
