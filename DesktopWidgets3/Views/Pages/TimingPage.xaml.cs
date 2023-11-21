using Microsoft.UI.Xaml.Controls;

using DesktopWidgets3.ViewModels.Pages;
using DesktopWidgets3.Views.SubPages;

namespace DesktopWidgets3.Views.Pages;

public sealed partial class TimingPage : Page
{
    public TimingViewModel ViewModel
    {
        get;
    }

    public TimingPage()
    {
        ViewModel = App.GetService<TimingViewModel>();
        InitializeComponent();

        ViewModel.SubNavigationService.SetFrame(GetType(), NavigationFrame);
        ViewModel.SubNavigationService.InitializeDefaultPage(typeof(StartSettingPage));
    }
}
