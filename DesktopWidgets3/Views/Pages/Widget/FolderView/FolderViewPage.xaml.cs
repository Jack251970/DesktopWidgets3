using Microsoft.UI.Xaml.Controls;

using DesktopWidgets3.ViewModels.Pages.Widget;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using DesktopWidgets3.Models.Widget.FolderView;

namespace DesktopWidgets3.Views.Pages.Widget;

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

    private void Toolbar_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        ViewModel.ShowRightTappedMenu(sender, e);
    }

    private async void FileList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement { DataContext: FolderViewFileItem item })
        {
            var filePath = item.FilePath;
            await ViewModel.FolderViewItemDoubleTapped(filePath);
        }
    }

    private void Toolbar_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        ViewModel.ToolbarDoubleTapped();
    }
}
