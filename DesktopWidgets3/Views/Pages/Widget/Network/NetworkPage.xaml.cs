using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace DesktopWidgets3.Views.Pages.Widgets;

public sealed partial class NetworkPage : Page
{
    public NetworkViewModel ViewModel
    {
        get;
    }

    public NetworkPage()
    {
        ViewModel = App.GetService<NetworkViewModel>();
        InitializeComponent();

        ViewModel.RegisterRightTappedMenu(ContentArea);
    }
}
