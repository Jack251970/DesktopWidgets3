using Microsoft.UI.Xaml.Controls;

using DesktopWidgets3.ViewModels.WidgetsPages.Folder;
using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Views.WidgetPages.Folder;

public sealed partial class FolderViewPage : Page
{
    public FolderViewViewModel ViewModel
    {
        get;
    }

    public FolderViewPage()
    {
        ViewModel = App.GetService<FolderViewViewModel>();
        InitializeComponent();
    }

    private async void NavigateBackButtonClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.NavigateBackButtonClick();
    }

    private async void NavigateUpButtonClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.NavigateUpButtonClick();
    }

    private async void FolderViewItemClick(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        if (button == null)
        {
            return;
        }

        if (button.Tag is not string filePath)
        {
            return;
        }
        await ViewModel.FolderViewItemClick(filePath);
    }
}
