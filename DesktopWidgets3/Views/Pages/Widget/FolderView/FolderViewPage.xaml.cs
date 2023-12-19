using Microsoft.UI.Xaml.Controls;

using DesktopWidgets3.ViewModels.Pages.Widget;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using DesktopWidgets3.Models.Widget.FolderView;
using Microsoft.UI.Xaml.Media;

namespace DesktopWidgets3.Views.Pages.Widget;

public sealed partial class FolderViewPage : BaseLayoutPage
{
    public FolderViewViewModel ViewModel
    {
        get;
    }

    public FolderViewPage()
    {
        ViewModel = App.GetService<FolderViewViewModel>();
        InitializeComponent();

        ViewModel.NavigatedTo += (s, e) => { ItemContextMenuFlyout.Opened += ItemContextFlyout_Opening; };
        ViewModel.NavigatedFrom += (s, e) => { ItemContextMenuFlyout.Opened -= ItemContextFlyout_Opening; };
    }

    private void Toolbar_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        ViewModel.ShowRightTappedMenu(sender, e);
    }

    private void Toolbar_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        ViewModel.ToolbarDoubleTapped();
    }

    private async void FileList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement { DataContext: FolderViewFileItem item })
        {
            var filePath = item.FilePath;
            await ViewModel.FolderViewItemDoubleTapped(filePath);
        }
    }

    private void StackPanel_Loaded(object sender, RoutedEventArgs e)
    {
        // This is the best way I could find to set the context flyout, as doing it in the styles isn't possible
        // because you can't use bindings in the setters
        var item = VisualTreeHelper.GetParent(sender as StackPanel);
        while (item is not ListViewItem)
        {
            item = VisualTreeHelper.GetParent(item);
        }
        if (item is ListViewItem itemContainer)
        {
            itemContainer.ContextFlyout = ItemContextMenuFlyout;
        }
    }
}
