using Microsoft.UI.Xaml.Controls;

using DesktopWidgets3.ViewModels.Pages;
using Microsoft.UI.Xaml;
using DesktopWidgets3.Models.Widget;

namespace DesktopWidgets3.Views.Pages;

public sealed partial class DashboardPage : Page
{
    public DashboardViewModel ViewModel
    {
        get;
    }

    public DashboardPage()
    {
        ViewModel = App.GetService<DashboardViewModel>();
        InitializeComponent();
    }

    private void AllWidgetsItemClick(object sender, RoutedEventArgs e)
    {
        var widgetType = (WidgetType)((Button)sender).Tag;
        ViewModel.AllWidgetsItemClick(widgetType);
    }
}
