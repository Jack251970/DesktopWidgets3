namespace DesktopWidgets3.Widget.Models.Main;

public class WidgetInitContext(
    WidgetMetadata metadata,
    ILogService logService,
    ISettingsService settingsService,
    IThemeService themeService,
    IWidgetService widgetService)
{
    public WidgetMetadata WidgetMetadata { get; private set; } = metadata;

    public ISettingsService SettingsService { get; private set; } = settingsService;

    public ILogService LogService { get; private set; } = logService;

    public IThemeService ThemeService { get; private set; } = themeService;

    public IWidgetService WidgetService { get; private set; } = widgetService;
}
