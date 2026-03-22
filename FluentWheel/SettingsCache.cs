#pragma warning disable VSEXTPREVIEW_SETTINGS
using Cloris.FluentWheel.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Settings;
using System.Runtime.CompilerServices;

namespace Cloris.FluentWheel;

internal static class SettingsCache
{
    private static SettingsCategoryObserver? _settingsCategoryObserver;
    private static IDisposable? _externalSettingsSubscription;

    public static event Action<string>? SettingsChanged;

    public static bool IsInitialized { get; private set; }

    public static int ScrollDuration { get => field; private set => SetProperty(ref field, value); }

    public static EasingMode ScrollEasingMode { get => field; private set => SetProperty(ref field, value); }

    public static int VerticalScrollRate { get => field; private set => SetProperty(ref field, value); }

    public static int HorizontalScrollRate { get => field; private set => SetProperty(ref field, value); }

    public static bool EnableLowLevelMouseHook { get => field; private set => SetProperty(ref field, value); }

    public static int ZoomDuration { get => field; private set => SetProperty(ref field, value); }

    public static EasingMode ZoomEasingMode { get => field; private set => SetProperty(ref field, value); }

    public static int LinesPerVerticalScroll { get => field; private set => SetProperty(ref field, value); } = 3;

    public static int CharsPerHorizontalScroll { get => field; private set => SetProperty(ref field, value); } = 10;

    public static double FastScrollMultiplier { get => field; private set => SetProperty(ref field, value); } = 5.0;

    public static async Task InitializeAsync(VisualStudioExtensibility extensibility, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        _settingsCategoryObserver = serviceProvider.GetRequiredService<SettingsCategoryObserver>();
        _settingsCategoryObserver.Changed += SettingsChangedAsync;

        var settings = extensibility.Settings();

        var settingValues = await settings.ReadEffectiveValuesAsync(
        [
            SettingsDefinition.ScrollDurationSetting,
            SettingsDefinition.ScrollEasingModeSetting,
            SettingsDefinition.VerticalScrollRateSetting,
            SettingsDefinition.HorizontalScrollRateSetting,
            SettingsDefinition.ZoomDurationSetting,
            SettingsDefinition.ZoomEasingModeSetting,
            SettingsDefinition.EnableLowLevelMouseHookSetting
        ], cancellationToken);

        ScrollDuration = settingValues.ValueOrDefault(SettingsDefinition.ScrollDurationSetting, SettingsDefinition.ScrollDurationSetting.DefaultValue);
        ScrollEasingMode = (EasingMode)Enum.Parse(typeof(EasingMode), settingValues.ValueOrDefault(SettingsDefinition.ScrollEasingModeSetting, SettingsDefinition.ScrollEasingModeSetting.DefaultValue));
        VerticalScrollRate = settingValues.ValueOrDefault(SettingsDefinition.VerticalScrollRateSetting, SettingsDefinition.VerticalScrollRateSetting.DefaultValue);
        HorizontalScrollRate = settingValues.ValueOrDefault(SettingsDefinition.HorizontalScrollRateSetting, SettingsDefinition.HorizontalScrollRateSetting.DefaultValue);
        EnableLowLevelMouseHook = settingValues.ValueOrDefault(SettingsDefinition.EnableLowLevelMouseHookSetting, SettingsDefinition.EnableLowLevelMouseHookSetting.DefaultValue);
        ZoomDuration = settingValues.ValueOrDefault(SettingsDefinition.ZoomDurationSetting, SettingsDefinition.ZoomDurationSetting.DefaultValue);
        ZoomEasingMode = (EasingMode)Enum.Parse(typeof(EasingMode), settingValues.ValueOrDefault(SettingsDefinition.ZoomEasingModeSetting, SettingsDefinition.ZoomEasingModeSetting.DefaultValue));

        await LoadExternalSettingsAsync(settings, cancellationToken);
        IsInitialized = true;
    }

    private static Task SettingsChangedAsync(SettingsCategorySnapshot arg)
    {
        ScrollDuration = arg.ScrollDurationSetting.ValueOrDefault(SettingsDefinition.ScrollDurationSetting.DefaultValue);
        ScrollEasingMode = (EasingMode)Enum.Parse(typeof(EasingMode), arg.ScrollEasingModeSetting.ValueOrDefault(SettingsDefinition.ScrollEasingModeSetting.DefaultValue));
        VerticalScrollRate = arg.VerticalScrollRateSetting.ValueOrDefault(SettingsDefinition.VerticalScrollRateSetting.DefaultValue);
        HorizontalScrollRate = arg.HorizontalScrollRateSetting.ValueOrDefault(SettingsDefinition.HorizontalScrollRateSetting.DefaultValue);
        EnableLowLevelMouseHook = arg.EnableLowLevelMouseHookSetting.ValueOrDefault(SettingsDefinition.EnableLowLevelMouseHookSetting.DefaultValue);
        ZoomDuration = arg.ZoomDurationSetting.ValueOrDefault(SettingsDefinition.ZoomDurationSetting.DefaultValue);
        ZoomEasingMode = (EasingMode)Enum.Parse(typeof(EasingMode), arg.ZoomEasingModeSetting.ValueOrDefault(SettingsDefinition.ZoomEasingModeSetting.DefaultValue));

        return Task.CompletedTask;
    }

    private static async Task LoadExternalSettingsAsync(SettingsExtensibility settings, CancellationToken cancellationToken)
    {
        var linesPerVerticalScrollSetting = SettingIdentifier.Custom("textEditor.advanced.scrolling.linesPerVerticalScroll");
        var charsPerHorizontalScrollSetting = SettingIdentifier.Custom("textEditor.advanced.scrolling.charsPerHorizontalScroll");
        var fastScrollMultiplierSetting = SettingIdentifier.Custom("textEditor.advanced.scrolling.fastScrollMultiplier");

        _externalSettingsSubscription = await settings.SubscribeAsync([linesPerVerticalScrollSetting, charsPerHorizontalScrollSetting, fastScrollMultiplierSetting], cancellationToken, ReadExternalSettings);

        void ReadExternalSettings(SettingValues? values)
        {
            if (values is null)
            {
                return;
            }

            if (values.TryGetValue(linesPerVerticalScrollSetting, out var settingValue) && settingValue is SettingValue<int> linesPerVerticalScrollValue)
            {
                LinesPerVerticalScroll = linesPerVerticalScrollValue.Value;
            }

            if (values.TryGetValue(charsPerHorizontalScrollSetting, out settingValue) && settingValue is SettingValue<int> charsPerHorizontalScrollValue)
            {
                CharsPerHorizontalScroll = charsPerHorizontalScrollValue.Value;
            }

            if (values.TryGetValue(fastScrollMultiplierSetting, out settingValue) && settingValue is SettingValue<decimal> fastScrollMultiplierValue)
            {
                FastScrollMultiplier = (double)fastScrollMultiplierValue.Value;
            }
        }
    }

    public static void Cleanup()
    {
        if (_settingsCategoryObserver is not null)
        {
            _settingsCategoryObserver.Changed -= SettingsChangedAsync;
            _settingsCategoryObserver.Dispose();
            _settingsCategoryObserver = null;
        }

        _externalSettingsSubscription?.Dispose();
        _externalSettingsSubscription = null;

        IsInitialized = false;
    }

    private static void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null!)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            SettingsChanged?.Invoke(propertyName);
        }
    }
}
