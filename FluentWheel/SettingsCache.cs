#pragma warning disable VSEXTPREVIEW_SETTINGS
using Cloris.FluentWheel.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Extensibility;

namespace Cloris.FluentWheel;

internal static class SettingsCache
{
    private static SettingsCategoryObserver? _settingsCategoryObserver;

    public static bool IsInitialized { get; private set; }

    public static int ScrollDuration { get; private set; }

    public static EasingMode ScrollEasingMode { get; private set; }

    public static int VerticalScrollRate { get; private set; }

    public static int HorizontalScrollRate { get; private set; }

    public static int ZoomDuration { get; private set; }

    public static EasingMode ZoomEasingMode { get; private set; }

    public static async Task InitializeAsync(VisualStudioExtensibility extensibility, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        _settingsCategoryObserver = serviceProvider.GetRequiredService<SettingsCategoryObserver>();
        _settingsCategoryObserver.Changed += SettingsChangedAsync;

        var settings = await extensibility.Settings().ReadEffectiveValuesAsync(
        [
            SettingsDefinition.ScrollDurationSetting,
            SettingsDefinition.ScrollEasingModeSetting,
            SettingsDefinition.VerticalScrollRateSetting,
            SettingsDefinition.HorizontalScrollRateSetting,
            SettingsDefinition.ZoomDurationSetting,
            SettingsDefinition.ZoomEasingModeSetting
        ], cancellationToken);

        ScrollDuration = settings.ValueOrDefault(SettingsDefinition.ScrollDurationSetting, SettingsDefinition.ScrollDurationSetting.DefaultValue);
        ScrollEasingMode = (EasingMode)Enum.Parse(typeof(EasingMode), settings.ValueOrDefault(SettingsDefinition.ScrollEasingModeSetting, SettingsDefinition.ScrollEasingModeSetting.DefaultValue));
        VerticalScrollRate = settings.ValueOrDefault(SettingsDefinition.VerticalScrollRateSetting, SettingsDefinition.VerticalScrollRateSetting.DefaultValue);
        HorizontalScrollRate = settings.ValueOrDefault(SettingsDefinition.HorizontalScrollRateSetting, SettingsDefinition.HorizontalScrollRateSetting.DefaultValue);
        ZoomDuration = settings.ValueOrDefault(SettingsDefinition.ZoomDurationSetting, SettingsDefinition.ZoomDurationSetting.DefaultValue);
        ZoomEasingMode = (EasingMode)Enum.Parse(typeof(EasingMode), settings.ValueOrDefault(SettingsDefinition.ZoomEasingModeSetting, SettingsDefinition.ZoomEasingModeSetting.DefaultValue));

        IsInitialized = true;
    }

    private static Task SettingsChangedAsync(SettingsCategorySnapshot arg)
    {
        ScrollDuration = arg.ScrollDurationSetting.ValueOrDefault(SettingsDefinition.ScrollDurationSetting.DefaultValue);
        ScrollEasingMode = (EasingMode)Enum.Parse(typeof(EasingMode), arg.ScrollEasingModeSetting.ValueOrDefault(SettingsDefinition.ScrollEasingModeSetting.DefaultValue));
        VerticalScrollRate = arg.VerticalScrollRateSetting.ValueOrDefault(SettingsDefinition.VerticalScrollRateSetting.DefaultValue);
        HorizontalScrollRate = arg.HorizontalScrollRateSetting.ValueOrDefault(SettingsDefinition.HorizontalScrollRateSetting.DefaultValue);
        ZoomDuration = arg.ZoomDurationSetting.ValueOrDefault(SettingsDefinition.ZoomDurationSetting.DefaultValue);
        ZoomEasingMode = (EasingMode)Enum.Parse(typeof(EasingMode), arg.ZoomEasingModeSetting.ValueOrDefault(SettingsDefinition.ZoomEasingModeSetting.DefaultValue));

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
