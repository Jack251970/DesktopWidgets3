namespace DesktopWidgets3.Widget.Contracts.Main;

public interface IWidget : IAsyncWidget
{
    void InitWidget(WidgetInitContext context);

    Task IAsyncWidget.InitWidgetAsync(WidgetInitContext context) => Task.Run(() => InitWidget(context));

    void InitWidgetInstance(bool firstWidget);

    Task IAsyncWidget.InitWidgetInstanceAsync(bool firstWidget) => Task.Run(() => InitWidgetInstance(firstWidget));
}
