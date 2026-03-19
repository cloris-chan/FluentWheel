namespace Cloris.FluentWheel;

internal static class Easing
{
    public static double Apply(EasingMode mode, double t) => mode switch
    {
        EasingMode.EaseIn => t * t * t,
        EasingMode.EaseOut => 1.0 - Math.Pow(1.0 - t, 3),
        EasingMode.EaseInOut => t < 0.5 ? 4.0 * t * t * t : 1.0 - Math.Pow(-2.0 * t + 2.0, 3) / 2.0,
        _ => t,
    };
}
