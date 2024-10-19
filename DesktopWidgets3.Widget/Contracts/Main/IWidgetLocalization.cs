namespace DesktopWidgets3.Widget;

/// <summary>
/// Widget localization interface.
/// </summary>
public interface IWidgetLocalization
{
    /// <summary>
    /// Get the localized widget group title.
    /// </summary>
    /// <returns>The localized widget group title.</returns>
    string GetLocalizedWidgetGroupTitle();

    /// <summary>
    /// Get the localized widget group description.
    /// </summary>
    /// <returns>The localized widget group description.</returns>
    string GetLocalizedWidgetGroupDescription();

    /// <summary>
    /// Get the localized widget title for one widget type.
    /// </summary>
    /// <param name="widgetType">The widget type that the title is for.</param>
    /// <returns>The localized widget title.</returns>
    string GetLocalizedWidgetTitle(string widgetType);

    /// <summary>
    /// Get the localized widget description for one widget type.
    /// </summary>
    /// <param name="widgetType">The widget type that the description is for.</param>
    /// <returns>The localized widget description.</returns>
    string GetLocalizedWidgetDescription(string widgetType);
}
