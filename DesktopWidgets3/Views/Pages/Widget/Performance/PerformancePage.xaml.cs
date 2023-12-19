using Microsoft.UI.Xaml.Controls;

using DesktopWidgets3.ViewModels.Pages.Widget;
using Microsoft.UI.Xaml.Input;

namespace DesktopWidgets3.Views.Pages.Widget;

public sealed partial class PerformancePage : Page
{
    public PerformanceViewModel ViewModel
    {
        get;
    }

    public PerformancePage()
    {
        ViewModel = App.GetService<PerformanceViewModel>();
        InitializeComponent();
    }

    private void ContentArea_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        ViewModel.ShowRightTappedMenu(sender, e);
    }
}
