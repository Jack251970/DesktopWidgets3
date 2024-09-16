namespace DesktopWidgets3.Widget.Models;

public class WidgetInitContext(
    WidgetMetadata metadata,
    ILocalizationService localizationService,
    ILogService logService,
    ISettingsService settingsService,
    IThemeService themeService,
    IWidgetService widgetService)
{
    public WidgetMetadata WidgetMetadata { get; private set; } = metadata;

    public ILocalizationService LocalizationService { get; private set; } = localizationService;

    public ILogService LogService { get; private set; } = logService;

    public ISettingsService SettingsService { get; private set; } = settingsService;

    public IThemeService ThemeService { get; private set; } = themeService;

    public IWidgetService WidgetService { get; private set; } = widgetService;
}
