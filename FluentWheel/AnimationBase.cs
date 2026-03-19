using System.Diagnostics;

namespace Cloris.FluentWheel;

internal abstract class AnimationBase
{
    protected long _timestamp;

    protected double _startValue;
    protected double _previousValue;
    protected double _targetValue;

    public bool IsAnimating { get; protected set; }

    protected abstract double Duration { get; }

    protected abstract EasingMode EasingMode { get; }

    protected abstract double Tolerance { get; }

    protected void BeginAnimation(double startValue, double targetValue)
    {
        _startValue = startValue;
        _previousValue = startValue;
        _targetValue = targetValue;

        if (Math.Abs(_targetValue - _startValue) < double.Epsilon)
        {
            Reset();
            return;
        }

        _timestamp = Stopwatch.GetTimestamp();
        IsAnimating = true;
    }

    protected (double Previous, double Next) GetCurrentValue()
    {
        var previousValue = _previousValue;
        var targetValue = _targetValue;
        if (!IsAnimating || Duration <= 0)
        {
            Reset();
            return (previousValue, targetValue);
        }

        var elapsedMilliseconds = Stopwatch.GetElapsedTime(_timestamp).TotalMilliseconds;

        if (elapsedMilliseconds >= Duration)
        {
            Reset();
            return (previousValue, targetValue);
        }

        var progress = Easing.Apply(EasingMode, elapsedMilliseconds / Duration);
        var nextValue = InterpolateValue(progress);

        if ((targetValue > _startValue ? targetValue - nextValue : nextValue - targetValue) < Tolerance)
        {
            Reset();
            return (previousValue, targetValue);
        }

        _previousValue = nextValue;
        return (previousValue, nextValue);
    }

    protected abstract double InterpolateValue(double progress);

    protected void Reset()
    {
        _timestamp = 0;
        _startValue = 0;
        _previousValue = 0;
        _targetValue = 0;
        IsAnimating = false;
    }
}
