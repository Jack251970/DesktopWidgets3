using DesktopWidgets3.ViewModels.Pages.Widget.Settings;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Views.Pages.Widget.Settings;

public sealed partial class DiskSettingsPage : Page
{
    public DiskSettingsViewModel ViewModel
    {
        get;
    }

    public DiskSettingsPage()
    {
        ViewModel = App.GetService<DiskSettingsViewModel>();
        InitializeComponent();
    }
}
