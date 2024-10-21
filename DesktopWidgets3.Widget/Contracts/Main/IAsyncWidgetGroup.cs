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
    /// <param name="widgetInitContext">Context to provide information and functions for the widget group.</param>
    /// <returns>The task that represents the asynchronous operation.</returns>
    Task InitWidgetGroupAsync(IWidgetInitContext widgetInitContext);

    /// <summary>
    /// Create and get the widget content for one widget instance.
    /// </summary>
    /// <param name="widgetContext">Context to provide information for one widget instance.</param>
    /// <param name="resourceDictionary">
    /// A resource dictionary used for the widget content.
    /// It consists of the string resources that are used by the widget content.
    /// </param>
    /// <returns>The widget content.</returns>
    FrameworkElement CreateWidgetContent(IWidgetContext widgetContext, ResourceDictionary? resourceDictionary);

    /// <summary>
    /// Delete one widget instance.
    /// </summary>
    /// <param name="widgetId">The unique identifier of the widget instance.</param>
    /// <param name="widgetSettings">Settings of the widget instance.</param>
    void DeleteWidget(string widgetId, BaseWidgetSettings widgetSettings);

    /// <summary>
    /// Activate one widget instance.
    /// </summary>
    /// <param name="widgetContext">Context to provide information for one widget instance.</param>
    void ActivateWidget(IWidgetContext widgetContext);

    /// <summary>
    /// Deactivate one widget instance.
    /// </summary>
    /// <param name="widgetId">The unique identifier of the widget instance.</param>
    void DeactivateWidget(string widgetId);
}
