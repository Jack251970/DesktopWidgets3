namespace DesktopWidgets3.Widget.Contracts.Main;

public interface IAsyncWidgetPin
{
    Task WidgetPinningAsync(bool firstWidget);

    Task WidgetUnpinnedAsync(bool lastWidget);
}
