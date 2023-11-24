using Microsoft.UI.Xaml.Controls;

using DesktopWidgets3.ViewModels.Pages;

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
    }
}
