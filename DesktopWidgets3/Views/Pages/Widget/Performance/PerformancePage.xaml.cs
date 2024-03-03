using Microsoft.UI.Xaml.Controls;

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

        ViewModel.RegisterRightTappedMenu(ContentArea);
    }
}
