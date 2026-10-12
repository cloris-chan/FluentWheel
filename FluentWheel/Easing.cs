using System.Windows;
using System.Windows.Media.Animation;

namespace Cloris.FluentWheel;

internal static class Easing
{
    public static IEasingFunction CreateFunction(double? startSlope = null) => startSlope.HasValue
        ? new ContinuationEase(startSlope.Value)
        : new CubicEase { EasingMode = EasingMode.EaseOut };

    public static double GetDerivative(IEasingFunction function, double progress) => function is ContinuationEase continuation
        ? continuation.GetDerivative(progress)
        : 3 * (1 - progress) * (1 - progress);

    private sealed class ContinuationEase : EasingFunctionBase
    {
        private readonly double _startSlope;

        public ContinuationEase(double startSlope)
        {
            _startSlope = startSlope;
            EasingMode = EasingMode.EaseIn;
        }

        protected override double EaseInCore(double t) => t * (_startSlope + t * (3 - 2 * _startSlope + t * (_startSlope - 2)));

        public double GetDerivative(double t) => Math.Max(0, _startSlope + t * (6 - 4 * _startSlope + t * (3 * _startSlope - 6)));

        protected override Freezable CreateInstanceCore() => new ContinuationEase(_startSlope);
    }
}
