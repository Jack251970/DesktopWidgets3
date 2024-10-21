namespace DesktopWidgets3.Core.Widgets.Models.WidgetContexts;

public class WidgetInitContext : IWidgetInitContext
{
    public required WidgetGroupMetadata WidgetGroupMetadata { get; set; }

    public required ILocalizationService LocalizationService { get; set; }

    public required ILogService LogService { get; set; }

    public required ISettingsService SettingsService { get; set; }

    public required IThemeService ThemeService { get; set; }

    public required IWidgetService WidgetService { get; set; }
}
