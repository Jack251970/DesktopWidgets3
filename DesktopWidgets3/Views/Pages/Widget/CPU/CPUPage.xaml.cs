using Microsoft.UI.Xaml.Controls;

using DesktopWidgets3.ViewModels.Pages.Widget.CPU;
using Microsoft.UI.Xaml.Input;

namespace DesktopWidgets3.Views.Pages.Widget.CPU;

public sealed partial class CPUPage : Page
{
    public CPUViewModel ViewModel
    {
        get;
    }

    public CPUPage()
    {
        ViewModel = App.GetService<CPUViewModel>();
        InitializeComponent();
    }

    private void ContentArea_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        ViewModel.ShowRightTappedMenu(sender, e);
    }
}
