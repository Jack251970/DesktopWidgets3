using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Views.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsPageViewModel ViewModel { get; }

    public SettingsPage()
    {
        ViewModel = DependencyExtensions.GetRequiredService<SettingsPageViewModel>();
        InitializeComponent();
    }
}
