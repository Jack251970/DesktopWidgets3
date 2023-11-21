using Microsoft.UI.Xaml.Controls;
using DesktopWidgets3.ViewModels.SubPages;

namespace DesktopWidgets3.Views.SubPages;

public sealed partial class MainTimingPage : Page
{
    public MainTimingViewModel ViewModel
    {
        get;
    }

    public MainTimingPage()
    {
        ViewModel = App.GetService<MainTimingViewModel>();
        InitializeComponent();
    }
}
