using Microsoft.UI.Xaml.Controls;

using Microsoft.UI.Xaml.Input;

namespace DesktopWidgets3.Views.Pages.Widgets;

public sealed partial class ClockPage : Page
{
    public ClockViewModel ViewModel
    {
        get;
    }

    public ClockPage()
    {
        ViewModel = App.GetService<ClockViewModel>();
        InitializeComponent();

        ViewModel.RegisterRightTappedMenu(ContentArea);
    }
}
