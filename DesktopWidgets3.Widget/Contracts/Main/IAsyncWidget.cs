using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Widget.Contracts.Main;

public interface IAsyncWidget
{
    FrameworkElement CreateWidgetFrameworkElement(ResourceDictionary? resourceDictionary);

    Task InitWidgetAsync(WidgetInitContext context);
}
