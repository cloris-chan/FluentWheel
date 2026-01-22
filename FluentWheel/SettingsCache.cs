#pragma warning disable VSEXTPREVIEW_SETTINGS
using Cloris.FluentWheel.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Extensibility;

namespace Cloris.FluentWheel;

internal static class SettingsCache
{
    private static SettingsCategoryObserver? _settingsCategoryObserver;

    public static bool IsInitialized { get; private set; }

    public static int ScrollDuration { get; private set; } = 100;

    public static int VerticalScrollRate { get; private set; } = 100;

    public static int HorizontalScrollRate { get; private set; } = 100;

    public static int ZoomDuration { get; private set; } = 100;

    public static async Task InitializeAsync(VisualStudioExtensibility extensibility, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        _settingsCategoryObserver = serviceProvider.GetRequiredService<SettingsCategoryObserver>();
        _settingsCategoryObserver.Changed += SettingsChangedAsync;

        var settings = await extensibility.Settings().ReadEffectiveValuesAsync(
        [
            SettingsDefinition.ScrollDurationSetting,
            SettingsDefinition.VerticalScrollRateSetting,
            SettingsDefinition.HorizontalScrollRateSetting,
            SettingsDefinition.ZoomDurationSetting
        ], cancellationToken);

        ScrollDuration = settings.ValueOrDefault(SettingsDefinition.ScrollDurationSetting, 100);
        VerticalScrollRate = settings.ValueOrDefault(SettingsDefinition.VerticalScrollRateSetting, 100);
        HorizontalScrollRate = settings.ValueOrDefault(SettingsDefinition.HorizontalScrollRateSetting, 100);
        ZoomDuration = settings.ValueOrDefault(SettingsDefinition.ZoomDurationSetting, 100);

        IsInitialized = true;
    }

    private static Task SettingsChangedAsync(SettingsCategorySnapshot arg)
    {
        ScrollDuration = arg.ScrollDurationSetting.ValueOrDefault(100);
        VerticalScrollRate = arg.VerticalScrollRateSetting.ValueOrDefault(100);
        HorizontalScrollRate = arg.HorizontalScrollRateSetting.ValueOrDefault(100);
        ZoomDuration = arg.ZoomDurationSetting.ValueOrDefault(100);
        return Task.CompletedTask;
    }

    public static void Cleanup()
    {
        if (_settingsCategoryObserver is not null)
        {
            _settingsCategoryObserver.Changed -= SettingsChangedAsync;
            _settingsCategoryObserver.Dispose();
            _settingsCategoryObserver = null;
        }

        IsInitialized = false;
    }
}
