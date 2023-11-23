using Microsoft.UI.Xaml.Controls;

using DesktopWidgets3.ViewModels.Pages;
using DesktopWidgets3.Contracts.ViewModels;

namespace DesktopWidgets3.Views.Pages;

public sealed partial class DashboardPage : Page, IRefreshablePage
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

    public void RefreshEnabledState()
    {
        ViewModel.WidgetsEnabledChanged();
    }
}
