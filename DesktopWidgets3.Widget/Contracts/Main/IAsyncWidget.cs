using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Widget.Contracts.Main;

public interface IAsyncWidget
{
    FrameworkElement CreateWidgetFrameworkElement();

    Task InitWidgetAsync(WidgetInitContext context);

    Task EnableWidgetAsync(bool firstWidget);

    Task DisableWidgetAsync(bool lastWidget);
}
