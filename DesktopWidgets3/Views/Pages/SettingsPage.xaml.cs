using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Views.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        ViewModel = DependencyExtensions.GetRequiredService<SettingsViewModel>();
        InitializeComponent();
    }
}
