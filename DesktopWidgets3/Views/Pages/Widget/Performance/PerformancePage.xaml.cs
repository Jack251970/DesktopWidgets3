using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace DesktopWidgets3.Views.Pages.Widgets;

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
