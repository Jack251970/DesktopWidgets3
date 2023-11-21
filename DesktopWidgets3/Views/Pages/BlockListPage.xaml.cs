using Microsoft.UI.Xaml.Controls;

using DesktopWidgets3.ViewModels.Pages;

namespace DesktopWidgets3.Views.Pages;

public sealed partial class BlockListPage : Page
{
    public BlockListViewModel ViewModel
    {
        get;
    }

    public BlockListPage()
    {
        ViewModel = App.GetService<BlockListViewModel>();
        InitializeComponent();
    }
}
