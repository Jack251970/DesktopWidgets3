using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Widget;

/// <summary>
/// Widget group model.
/// </summary>
public interface IAsyncWidgetGroup
{
    /// <summary>
    /// Initialize the widget group model asynchrously.
    /// </summary>
    /// <param name="context">A object that provides information and functions for the widget group.</param>
    /// <returns>A task that represents the asynchronous operation to get the list of the widget types that the widget group contains.</returns>
    Task<List<string>> InitWidgetGroupAsync(WidgetInitContext widgetInitContext);

    /// <summary>
    /// Get the widget content for one widget type.
    /// </summary>
    /// <param name="widgetType">The widget type that the content is for.</param>
    /// <param name="resourceDictionary">
    /// The resource dictionary to use for the widget content.
    /// It consists of the string resources that are used by the widget content.
    /// </param>
    /// <returns>The widget content.</returns>
    FrameworkElement GetWidgetContent(string widgetType, ResourceDictionary? resourceDictionary);

    /// <summary>
    /// Get the localized widget title for one widget type.
    /// </summary>
    /// <param name="widgetType">The widget type that the name is for.</param>
    /// <returns>The localized widget title.</returns>
    string GetLocalizatedTitle(string widgetType);

    /// <summary>
    /// Get the localized widget description for one widget type.
    /// </summary>
    /// <param name="widgetType">The widget type that the description is for.</param>
    /// <returns>The localized widget description.</returns>
    string GetLocalizatedDescription(string widgetType);
}
