using DesktopWidgets3.ViewModels.Pages.Widget.Settings;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Views.Pages.Widget.Settings;

public sealed partial class NetworkSettingsPage : Page
{
    public NetworkSettingsViewModel ViewModel
    {
        get;
    }

    public NetworkSettingsPage()
    {
        ViewModel = App.GetService<NetworkSettingsViewModel>();
        InitializeComponent();
    }
}
