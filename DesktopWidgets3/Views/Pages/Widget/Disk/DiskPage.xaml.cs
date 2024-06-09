using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Views.Pages.Widgets;

public sealed partial class DiskPage : Page
{
    public DiskViewModel ViewModel { get; }

    public DiskPage()
    {
        ViewModel = App.GetService<DiskViewModel>();
        InitializeComponent();

        ViewModel.RegisterRightTappedMenu(ContentArea);
    }
}
