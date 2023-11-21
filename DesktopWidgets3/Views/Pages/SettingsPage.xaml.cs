using Microsoft.UI.Xaml.Controls;

using DesktopWidgets3.ViewModels.Pages;

namespace DesktopWidgets3.Views.Pages;

// TODO: Set the URL for your privacy policy by updating Settings_PrivacyTermsLink.NavigateUri in Resources.resw.
public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel
    {
        get;
    }

    public SettingsPage()
    {
        ViewModel = App.GetService<SettingsViewModel>();
        InitializeComponent();
    }
}
