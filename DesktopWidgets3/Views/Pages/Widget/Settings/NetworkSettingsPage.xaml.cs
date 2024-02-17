using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Views.Pages.Widgets.Settings;

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
