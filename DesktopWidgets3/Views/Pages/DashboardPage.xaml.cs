using Microsoft.UI.Xaml.Controls;

using DesktopWidgets3.ViewModels.Pages;

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
}
