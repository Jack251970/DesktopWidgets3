using Microsoft.UI.Xaml.Controls;

using DesktopWidgets3.ViewModels.Pages.Widget.Network;
using Microsoft.UI.Xaml.Input;

namespace DesktopWidgets3.Views.Pages.Widget.Network;

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
    }

    private void ContentArea_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        ViewModel.ShowRightTappedMenu(sender, e);
    }
}
