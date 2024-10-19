namespace DesktopWidgets3.Widget;

/// <summary>
/// Widget localization interface.
/// </summary>
public interface IWidgetLocalization
{
    /// <summary>
    /// Get the localized widget group name.
    /// </summary>
    /// <returns>The localized widget group name.</returns>
    string GetLocalizedWidgetGroupName();

    /// <summary>
    /// Get the localized widget group description.
    /// </summary>
    /// <returns>The localized widget group description.</returns>
    string GetLocalizedWidgetGroupDescription();

    /// <summary>
    /// Get the localized widget name for one widget type.
    /// </summary>
    /// <param name="widgetType">The widget type that the name is for.</param>
    /// <returns>The localized widget name.</returns>
    string GetLocalizedWidgetName(string widgetType);

    /// <summary>
    /// Get the localized widget description for one widget type.
    /// </summary>
    /// <param name="widgetType">The widget type that the description is for.</param>
    /// <returns>The localized widget description.</returns>
    string GetLocalizedWidgetDescription(string widgetType);
}
