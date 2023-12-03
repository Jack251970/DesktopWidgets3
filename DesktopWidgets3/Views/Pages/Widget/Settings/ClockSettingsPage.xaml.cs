using DesktopWidgets3.ViewModels.Pages.Widget.Settings;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Views.Pages.Widget.Settings;

public sealed partial class ClockSettingsPage : Page
{
    public ClockSettingsViewModel ViewModel
    {
        get;
    }

    public ClockSettingsPage()
    {
        ViewModel = App.GetService<ClockSettingsViewModel>();
        InitializeComponent();
    }
}
