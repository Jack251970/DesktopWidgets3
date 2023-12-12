using DesktopWidgets3.ViewModels.Pages.Widget.Settings;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Views.Pages.Widget.Settings;

public sealed partial class CPUSettingsPage : Page
{
    public CPUSettingsViewModel ViewModel
    {
        get;
    }

    public CPUSettingsPage()
    {
        ViewModel = App.GetService<CPUSettingsViewModel>();
        InitializeComponent();
    }
}
