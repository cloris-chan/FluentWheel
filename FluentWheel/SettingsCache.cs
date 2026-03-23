#pragma warning disable VSEXTPREVIEW_SETTINGS
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Settings;
using System.Runtime.CompilerServices;

namespace Cloris.FluentWheel;

internal static class SettingsCache
{
    private static IDisposable? _settingsSubscription;

    public static event Action<string>? SettingsChanged;

    public static int ScrollDuration { get => field; private set => SetProperty(ref field, value); }

    public static EasingMode ScrollEasingMode { get => field; private set => SetProperty(ref field, value); }

    public static int VerticalScrollRate { get => field; private set => SetProperty(ref field, value); }

    public static int HorizontalScrollRate { get => field; private set => SetProperty(ref field, value); }

    public static int ZoomDuration { get => field; private set => SetProperty(ref field, value); }

    public static EasingMode ZoomEasingMode { get => field; private set => SetProperty(ref field, value); }

    public static bool EnableLowLevelMouseHook { get => field; private set => SetProperty(ref field, value); }

    public static int LinesPerVerticalScroll { get => field; private set => SetProperty(ref field, value); } = 3;

    public static int CharsPerHorizontalScroll { get => field; private set => SetProperty(ref field, value); } = 10;

    public static double FastScrollMultiplier { get => field; private set => SetProperty(ref field, value); } = 5.0;

    public static async Task InitializeAsync(VisualStudioExtensibility extensibility, CancellationToken cancellationToken)
    {
        var linesPerVerticalScrollSetting = SettingIdentifier.Custom("textEditor.advanced.scrolling.linesPerVerticalScroll");
        var charsPerHorizontalScrollSetting = SettingIdentifier.Custom("textEditor.advanced.scrolling.charsPerHorizontalScroll");
        var fastScrollMultiplierSetting = SettingIdentifier.Custom("textEditor.advanced.scrolling.fastScrollMultiplier");

        _settingsSubscription = await extensibility.Settings().SubscribeAsync(
        [
            SettingsDefinition.ScrollDurationSetting.FullId,
            SettingsDefinition.ScrollEasingModeSetting.FullId,
            SettingsDefinition.VerticalScrollRateSetting.FullId,
            SettingsDefinition.HorizontalScrollRateSetting.FullId,
            SettingsDefinition.ZoomDurationSetting.FullId,
            SettingsDefinition.ZoomEasingModeSetting.FullId,
            SettingsDefinition.EnableLowLevelMouseHookSetting.FullId,
            linesPerVerticalScrollSetting,
            charsPerHorizontalScrollSetting,
            fastScrollMultiplierSetting
        ], cancellationToken, ReadSettings);

        void ReadSettings(SettingValues? values)
        {
            if (values is null)
            {
                return;
            }

            if (values.TryGetValue(SettingsDefinition.ScrollDurationSetting.FullId, out var scrollDurationValue))
            {
                ScrollDuration = scrollDurationValue.Value;
            }
            if (values.TryGetValue(SettingsDefinition.ScrollEasingModeSetting.FullId, out var scrollEasingModeValue) && Enum.TryParse<EasingMode>(scrollEasingModeValue.Value, out var scrollEasingMode))
            {
                ScrollEasingMode = scrollEasingMode;
            }
            if (values.TryGetValue(SettingsDefinition.VerticalScrollRateSetting.FullId, out var verticalScrollRateValue))
            {
                VerticalScrollRate = verticalScrollRateValue.Value;
            }
            if (values.TryGetValue(SettingsDefinition.HorizontalScrollRateSetting.FullId, out var horizontalScrollRateValue))
            {
                HorizontalScrollRate = horizontalScrollRateValue.Value;
            }
            if (values.TryGetValue(SettingsDefinition.ZoomDurationSetting.FullId, out var zoomDurationValue))
            {
                ZoomDuration = zoomDurationValue.Value;
            }
            if (values.TryGetValue(SettingsDefinition.ZoomEasingModeSetting.FullId, out var zoomEasingModeValue) && Enum.TryParse<EasingMode>(zoomEasingModeValue.Value, out var zoomEasingMode))
            {
                ZoomEasingMode = zoomEasingMode;
            }
            if (values.TryGetValue(SettingsDefinition.EnableLowLevelMouseHookSetting.FullId, out var enableLowLevelMouseHookValue))
            {
                EnableLowLevelMouseHook = enableLowLevelMouseHookValue.Value;
            }

            if (values.TryGetValue(linesPerVerticalScrollSetting, out var externalSettingValue) && externalSettingValue is SettingValue<int> linesPerVerticalScrollValue)
            {
                LinesPerVerticalScroll = linesPerVerticalScrollValue.Value;
            }
            if (values.TryGetValue(charsPerHorizontalScrollSetting, out externalSettingValue) && externalSettingValue is SettingValue<int> charsPerHorizontalScrollValue)
            {
                CharsPerHorizontalScroll = charsPerHorizontalScrollValue.Value;
            }
            if (values.TryGetValue(fastScrollMultiplierSetting, out externalSettingValue) && externalSettingValue is SettingValue<decimal> fastScrollMultiplierValue)
            {
                FastScrollMultiplier = (double)fastScrollMultiplierValue.Value;
            }
        }
    }

    public static void Cleanup()
    {
        _settingsSubscription?.Dispose();
        _settingsSubscription = null;
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
