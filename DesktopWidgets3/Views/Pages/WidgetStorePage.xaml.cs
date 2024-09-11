using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Views.Pages;

public sealed partial class WidgetStorePage : Page
{
    public WidgetStoreViewModel ViewModel { get; }

    public WidgetStorePage()
    {
        ViewModel = App.GetService<WidgetStoreViewModel>();
        InitializeComponent();
    }

    private void MenuFlyoutItemInstallWidget_Click(object sender, RoutedEventArgs e)
    {
        // TODO
        if (sender is FrameworkElement element)
        {
            var widgetId = WidgetProperties.GetId(element);
        }
    }

    private void MenuFlyoutItemUninstallWidget_Click(object sender, RoutedEventArgs e)
    {
        // TODO
        if (sender is FrameworkElement element)
        {
            var widgetId = WidgetProperties.GetId(element);
        }
    }
}
