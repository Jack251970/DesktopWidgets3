using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Widget;

/// <summary>
/// Interface to provide settings storage for a widget group.
/// </summary>
public interface IWidgetGroupSetting
{
    /// <summary>
    /// Get the widget settings for one widget type.
    /// </summary>
    /// <param name="widgetType">The widget type that the settings is for.</param>
    /// <returns>The default widget settings model.</returns>
    BaseWidgetSettings GetDefaultSettings(string widgetType);

    /// <summary>
    /// Create and get the widget setting content for one widget instance.
    /// </summary>
    /// <param name="widgetType">The widget type that the content is for.</param>
    /// <param name="resourceDictionary">
    /// A resource dictionary used for the widget content.
    /// It consists of the string resources that are used by the widget setting content.
    /// </param>
    /// <returns>The widget setting content.</returns>
    FrameworkElement CreateWidgetSettingContent(string widgetType, ResourceDictionary? resourceDictionary);
}
