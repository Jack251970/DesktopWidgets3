namespace DesktopWidgets3.Widget.Contracts.Main;

public interface IWidget : IAsyncWidget
{
    void InitWidget(WidgetInitContext context);

    Task IAsyncWidget.InitWidgetAsync(WidgetInitContext context) => Task.Run(() => InitWidget(context));

    void EnableWidget(bool firstWidget);

    Task IAsyncWidget.EnableWidgetAsync(bool firstWidget) => Task.Run(() => EnableWidgetAsync(firstWidget));

    void DisableWidget(bool lastWidget);

    Task IAsyncWidget.DisableWidgetAsync(bool lastWidget) => Task.Run(() => DisableWidgetAsync(lastWidget));
}
