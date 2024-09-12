namespace DesktopWidgets3.Widget.Contracts.Main;

public interface IAsyncWidgetEnableDisable
{
    Task EnableWidgetAsync(bool firstWidget);

    Task DisableWidgetAsync(bool lastWidget);
}
