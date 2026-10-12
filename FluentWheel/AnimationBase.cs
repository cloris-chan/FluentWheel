using System.Windows.Media.Animation;

namespace Cloris.FluentWheel;

internal abstract class AnimationBase : IDisposable
{
    private AnimationClock? _clock;
    private double _animatedValue;
    private bool _disposed;

    protected double _targetValue;

    public double AppliedValue { get; private set; }

    public bool IsAnimating => _clock is not null;

    public bool HasPendingUpdate => !_disposed && (IsAnimating || _animatedValue != AppliedValue);

    protected abstract double Duration { get; }

    protected void BeginAnimation(double startValue, double targetValue)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }

        var duration = (double)Duration;
        if (duration > 0 && _clock is not null && targetValue == _targetValue)
        {
            return;
        }

        var previousClock = _clock;
        var isContinuing = previousClock?.CurrentState == ClockState.Active;
        var velocity = 0.0;
        if (isContinuing)
        {
            var previousAnimation = (DoubleAnimation)previousClock!.Timeline;
            velocity = (previousAnimation.To!.Value - previousAnimation.From!.Value)
                * Easing.GetDerivative(previousAnimation.EasingFunction, previousClock.CurrentProgress!.Value)
                / previousAnimation.Duration.TimeSpan.TotalMilliseconds;
        }

        RemoveClock();
        _animatedValue = startValue;
        _targetValue = targetValue;

        if (duration <= 0 || startValue == targetValue)
        {
            _animatedValue = targetValue;
            return;
        }

        var from = ToAnimationValue(startValue);
        var to = ToAnimationValue(targetValue);
        var distance = to - from;
        var preserveVelocity = isContinuing && (velocity == 0 || Math.Sign(velocity) == Math.Sign(distance));
        if (preserveVelocity && velocity != 0)
        {
            duration = Math.Min(duration, 3 * Math.Abs(distance / velocity));
        }

        var animationDuration = TimeSpan.FromTicks(Math.Max(1, (long)(duration * TimeSpan.TicksPerMillisecond)));
        double? startSlope = preserveVelocity ? Math.Min(3, velocity * animationDuration.TotalMilliseconds / distance) : null;
        var animation = new DoubleAnimation(from, to, animationDuration)
        {
            EasingFunction = Easing.CreateFunction(startSlope),
            FillBehavior = FillBehavior.HoldEnd,
        };
        animation.Freeze();
        _clock = (AnimationClock)animation.CreateClock(true);
        _clock.CurrentTimeInvalidated += ClockTimeInvalidated;
        if (isContinuing)
        {
            _clock.Controller.SeekAlignedToLastTick(TimeSpan.Zero, TimeSeekOrigin.BeginTime);
        }
        else
        {
            _clock.Controller.Begin();
        }
    }

    public double SampleValue()
    {
        if (_clock is not { } clock)
        {
            return _animatedValue;
        }

        if (clock.CurrentState == ClockState.Filling)
        {
            _animatedValue = _targetValue;
            RemoveClock();
        }
        else if (clock.CurrentState == ClockState.Active)
        {
            var animation = (DoubleAnimation)clock.Timeline;
            _animatedValue = FromAnimationValue(animation.GetCurrentValue(0, 0, clock));
        }

        return _animatedValue;
    }

    public void MarkApplied(double value)
    {
        if (_disposed)
        {
            return;
        }

        AppliedValue = value;
        if (!HasPendingUpdate)
        {
            Reset(value);
        }
    }

    public void CancelPendingUpdate()
    {
        if (!_disposed)
        {
            Reset(AppliedValue);
        }
    }

    protected virtual double ToAnimationValue(double value) => value;

    protected virtual double FromAnimationValue(double value) => value;

    protected void Reset(double value = 0)
    {
        RemoveClock();
        _animatedValue = value;
        _targetValue = value;
        AppliedValue = value;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Reset();
            _disposed = true;
        }
    }

    private void ClockTimeInvalidated(object? sender, EventArgs args)
    {
        if (ReferenceEquals(sender, _clock))
        {
            SampleValue();
        }
    }

    private void RemoveClock()
    {
        if (_clock is { } clock)
        {
            _clock = null;
            clock.CurrentTimeInvalidated -= ClockTimeInvalidated;
            clock.Controller.Remove();
        }
    }
}
