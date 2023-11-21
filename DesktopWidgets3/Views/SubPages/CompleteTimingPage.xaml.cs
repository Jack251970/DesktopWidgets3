using Microsoft.UI.Xaml.Controls;
using DesktopWidgets3.ViewModels.SubPages;

namespace DesktopWidgets3.Views.SubPages;

public sealed partial class CompleteTimingPage : Page
{
    public CompleteTimingViewModel ViewModel
    {
        get;
    }

    public CompleteTimingPage()
    {
        ViewModel = App.GetService<CompleteTimingViewModel>();
        InitializeComponent();
    }
}
