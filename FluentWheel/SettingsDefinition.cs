#pragma warning disable VSEXTPREVIEW_SETTINGS
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Settings;

namespace Cloris.FluentWheel;

internal static class SettingsDefinition
{
    [VisualStudioContribution]
    internal static SettingCategory SettingsCategory { get; } = new("fluentWheel", "%FluentWheel.Settings.Category%") { GenerateObserverClass = true };

    [VisualStudioContribution]
    internal static Setting.Integer ScrollDurationSetting { get; } = new("scrollDuration", "%FluentWheel.Settings.ScrollDuration%", SettingsCategory, 100) { Description = "%FluentWheel.Settings.ScrollDuration.Description%", Minimum = 0, Maximum = 1000 };

    [VisualStudioContribution]
    internal static Setting.Integer VerticalScrollRateSetting { get; } = new("verticalScrollRate", "%FluentWheel.Settings.VerticalScrollRate%", SettingsCategory, 100) { Description = "%FluentWheel.Settings.VerticalScrollRate.Description%", Minimum = -400, Maximum = 400 };

    [VisualStudioContribution]
    internal static Setting.Integer HorizontalScrollRateSetting { get; } = new("horizontalScrollRate", "%FluentWheel.Settings.HorizontalScrollRate%", SettingsCategory, 100) { Description = "%FluentWheel.Settings.HorizontalScrollRate.Description%", Minimum = -400, Maximum = 400 };

    [VisualStudioContribution]
    internal static Setting.Integer ZoomDurationSetting { get; } = new("zoomDuration", "%FluentWheel.Settings.ZoomDuration%", SettingsCategory, 100) { Description = "%FluentWheel.Settings.ZoomDuration.Description%", Minimum = 0, Maximum = 1000 };
}
