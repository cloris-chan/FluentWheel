#pragma warning disable VSEXTPREVIEW_SETTINGS
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Settings;

namespace Cloris.FluentWheel;

internal static class SettingsCache
{
    private static IDisposable? _settingsSubscription;

    public static int ScrollDuration { get; private set; } = 200;

    public static int VerticalScrollRate { get; private set; } = 100;

    public static int HorizontalScrollRate { get; private set; } = 100;

    public static int ZoomDuration { get; private set; } = 200;

    public static int LinesPerVerticalScroll { get; private set; } = 3;

    public static int CharsPerHorizontalScroll { get; private set; } = 10;

    public static double FastScrollMultiplier { get; private set; } = 5.0;

    public static async Task InitializeAsync(VisualStudioExtensibility extensibility, CancellationToken cancellationToken)
    {
        var linesPerVerticalScrollSetting = SettingIdentifier.Custom("textEditor.advanced.scrolling.linesPerVerticalScroll");
        var charsPerHorizontalScrollSetting = SettingIdentifier.Custom("textEditor.advanced.scrolling.charsPerHorizontalScroll");
        var fastScrollMultiplierSetting = SettingIdentifier.Custom("textEditor.advanced.scrolling.fastScrollMultiplier");

        _settingsSubscription = await extensibility.Settings().SubscribeAsync(
        [
            SettingsDefinition.ScrollDurationSetting.FullId,
            SettingsDefinition.VerticalScrollRateSetting.FullId,
            SettingsDefinition.HorizontalScrollRateSetting.FullId,
            SettingsDefinition.ZoomDurationSetting.FullId,
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
                ScrollDuration = scrollDurationValue.ValueOrDefault(ScrollDuration);
            }
            if (values.TryGetValue(SettingsDefinition.VerticalScrollRateSetting.FullId, out var verticalScrollRateValue))
            {
                VerticalScrollRate = verticalScrollRateValue.ValueOrDefault(VerticalScrollRate);
            }
            if (values.TryGetValue(SettingsDefinition.HorizontalScrollRateSetting.FullId, out var horizontalScrollRateValue))
            {
                HorizontalScrollRate = horizontalScrollRateValue.ValueOrDefault(HorizontalScrollRate);
            }
            if (values.TryGetValue(SettingsDefinition.ZoomDurationSetting.FullId, out var zoomDurationValue))
            {
                ZoomDuration = zoomDurationValue.ValueOrDefault(ZoomDuration);
            }

            if (values.TryGetValue(linesPerVerticalScrollSetting, out var externalSettingValue) && externalSettingValue is SettingValue<int> linesPerVerticalScrollValue)
            {
                LinesPerVerticalScroll = linesPerVerticalScrollValue.ValueOrDefault(LinesPerVerticalScroll);
            }
            if (values.TryGetValue(charsPerHorizontalScrollSetting, out externalSettingValue) && externalSettingValue is SettingValue<int> charsPerHorizontalScrollValue)
            {
                CharsPerHorizontalScroll = charsPerHorizontalScrollValue.ValueOrDefault(CharsPerHorizontalScroll);
            }
            if (values.TryGetValue(fastScrollMultiplierSetting, out externalSettingValue) && externalSettingValue is SettingValue<decimal> fastScrollMultiplierValue)
            {
                FastScrollMultiplier = (double)fastScrollMultiplierValue.ValueOrDefault((decimal)FastScrollMultiplier);
            }
        }
    }

    public static void Cleanup()
    {
        _settingsSubscription?.Dispose();
        _settingsSubscription = null;
    }
}
