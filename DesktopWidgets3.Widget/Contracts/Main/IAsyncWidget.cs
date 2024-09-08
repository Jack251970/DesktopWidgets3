using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.Contracts.Main;

public interface IAsyncWidget
{
    Page CreateWidgetPage();

    Task InitWidgetClassAsync(WidgetInitContext context);

    Task InitWidgetInstanceAsync(WidgetInitContext context);
}
