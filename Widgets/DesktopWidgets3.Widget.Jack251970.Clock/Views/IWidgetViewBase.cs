using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Widget.Jack251970.Clock.Views;

internal delegate IWidgetViewBase WidgetCreateDelegate(string widgetId, ResourceDictionary? resourceDictionary);
internal interface IWidgetViewBase : IDisposable
{
    bool IsActivated { get; }

    void Activate(IWidgetContext widgetContext);

    void Deactivate(string widgetId);

    void OnWidgetSettingsChanged(WidgetSettingsChangedArgs contextChangedArgs);
}
