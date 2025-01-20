namespace DesktopWidgets3.Widget.Jack251970.SystemInfo.Views;

internal delegate IWidgetViewBase WidgetCreateDelegate(string widgetId, HardwareInfoService HardwareInfoService);
internal interface IWidgetViewBase : IDisposable
{
    bool IsActivated { get; }

    void Activate(IWidgetContext widgetContext);

    void Deactivate(string widgetId);

    void OnWidgetSettingsChanged(WidgetSettingsChangedArgs contextChangedArgs);
}
