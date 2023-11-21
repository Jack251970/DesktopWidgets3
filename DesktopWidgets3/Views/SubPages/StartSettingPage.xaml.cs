using Microsoft.UI.Xaml.Controls;
using DesktopWidgets3.ViewModels.SubPages;

namespace DesktopWidgets3.Views.SubPages;

public sealed partial class StartSettingPage : Page
{
    public StartSettingViewModel ViewModel
    {
        get;
    }

    public StartSettingPage()
    {
        ViewModel = App.GetService<StartSettingViewModel>();
        InitializeComponent();
    }
}
