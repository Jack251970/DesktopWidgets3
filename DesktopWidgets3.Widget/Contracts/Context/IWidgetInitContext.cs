namespace DesktopWidgets3.Widget;

public interface IWidgetInitContext
{
    WidgetGroupMetadata WidgetGroupMetadata { get; }

    ILocalizationService LocalizationService { get; }

    ILogService LogService { get; }

    ISettingsService SettingsService { get; }

    IThemeService ThemeService { get; }

    IWidgetService WidgetService { get; }
}
