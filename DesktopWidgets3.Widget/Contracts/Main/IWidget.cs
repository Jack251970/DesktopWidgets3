namespace DesktopWidgets3.Widget.Contracts.Main;

public interface IWidget : IAsyncWidget
{
    void InitWidgetClass(WidgetInitContext context);

    void InitWidgetInstance(WidgetInitContext context);

    Task IAsyncWidget.InitWidgetClassAsync(WidgetInitContext context) => Task.Run(() => InitWidgetClass(context));

    Task IAsyncWidget.InitWidgetInstanceAsync(WidgetInitContext context) => Task.Run(() => InitWidgetInstance(context));
}
