namespace DesktopWidgets3.Widget;

/// <summary>
/// Context to provide information and functions for one widget group.
/// </summary>
public interface IWidgetInitContext
{
    /// <summary>
    /// Widget group metadata from widget.json file.
    /// </summary>
    WidgetGroupMetadata WidgetGroupMetadata { get; }

    /// <summary>
    /// Service to provide localization functions.
    /// </summary>
    ILocalizationService LocalizationService { get; }

    /// <summary>
    /// Service to provide settings functions.
    /// </summary>
    ISettingsService SettingsService { get; }

    /// <summary>
    /// Service to provide theme functions.
    /// </summary>
    IThemeService ThemeService { get; }

    /// <summary>
    /// Service to provide widget functions.
    /// </summary>
    IWidgetService WidgetService { get; }
}
