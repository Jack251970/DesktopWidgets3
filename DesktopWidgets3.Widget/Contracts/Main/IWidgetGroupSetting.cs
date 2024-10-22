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
    /// Create and get the widget setting content for one widgetsetting  instance.
    /// </summary>
    /// <param name="widgetContext">Context to provide information for one widget setting instance.</param>
    /// <param name="resourceDictionary">
    /// A resource dictionary used for the widget setting content.
    /// It consists of the string resources that are used by the widget setting content.
    /// </param>
    /// <returns>The widget setting content.</returns>
    FrameworkElement CreateWidgetSettingContent(IWidgetSettingContext widgetSettingContext, ResourceDictionary? resourceDictionary);

    /// <summary>
    /// Handle the widget settings changed event.
    /// This function is called when the widget settings of one widget instance or one widget setting instance is changed.
    /// </summary>
    /// <param name="settingsChangedArgs">The widget settings changed event arguments.</param>
    void OnWidgetSettingsChanged(WidgetSettingsChangedArgs settingsChangedArgs);
}
