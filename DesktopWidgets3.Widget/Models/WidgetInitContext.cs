namespace DesktopWidgets3.Widget;

/// <summary>
/// The widget initialization context.
/// </summary>
/// <param name="widgetGroupMetadata"></param>
/// <param name="localizationService"></param>
/// <param name="logService"></param>
/// <param name="settingsService"></param>
/// <param name="themeService"></param>
/// <param name="widgetService"></param>
public class WidgetInitContext(
    WidgetGroupMetadata widgetGroupMetadata,
    ILocalizationService localizationService,
    ILogService logService,
    ISettingsService settingsService,
    IThemeService themeService,
    IWidgetService widgetService)
{
    public WidgetGroupMetadata WidgetGroupMetadata { get; private set; } = widgetGroupMetadata;

    public ILocalizationService LocalizationService { get; private set; } = localizationService;

    public ILogService LogService { get; private set; } = logService;

    public ISettingsService SettingsService { get; private set; } = settingsService;

    public IThemeService ThemeService { get; private set; } = themeService;

    public IWidgetService WidgetService { get; private set; } = widgetService;
}
