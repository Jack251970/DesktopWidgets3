using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Views.Pages.Widgets.Settings;

public sealed partial class ClockSettingsPage : Page
{
    public ClockSettingsViewModel ViewModel { get; }

    public ClockSettingsPage()
    {
        ViewModel = App.GetService<ClockSettingsViewModel>();
        InitializeComponent();
    }
}
