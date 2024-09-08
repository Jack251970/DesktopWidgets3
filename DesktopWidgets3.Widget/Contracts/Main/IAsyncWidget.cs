using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Widget.Contracts.Main;

public interface IAsyncWidget
{
    FrameworkElement CreateWidgetPage();

    Task InitWidgetClassAsync(WidgetInitContext context);

    Task InitWidgetInstanceAsync(WidgetInitContext context);
}
