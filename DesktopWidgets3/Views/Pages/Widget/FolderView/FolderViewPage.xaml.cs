using Microsoft.UI.Xaml.Controls;

using DesktopWidgets3.ViewModels.Pages.Widget;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using DesktopWidgets3.Models.Widget.FolderView;
using Microsoft.UI.Xaml.Media;
using CommunityToolkit.WinUI.UI;

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

    #region widget context menu

    private void Toolbar_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        ViewModel.ShowRightTappedMenu(sender, e);
    }

    private void Toolbar_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        ViewModel.ToolbarDoubleTapped();
    }

    #endregion

    #region item double tapped event

    private async void FileList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement { DataContext: ListedItem item })
        {
            var filePath = item.ItemPath;
            await ViewModel.FolderViewItemDoubleTapped(filePath);
        }
    }

    #endregion

    #region item context menu

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

    #endregion

    #region select items

    private void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SelectedItems = FileList.SelectedItems.Cast<ListedItem>().Where(x => x is not null).ToList();

        if (e != null)
        {
            foreach (var item in e.AddedItems)
            {
                SetCheckboxSelectionState(item);
            }

            foreach (var item in e.RemovedItems)
            {
                SetCheckboxSelectionState(item);
            }
        }
    }

    private void SetCheckboxSelectionState(object item, ListViewItem? lviContainer = null)
    {
        var container = lviContainer ?? FileList.ContainerFromItem(item) as ListViewItem;
        if (container is not null)
        {
            var checkbox = container.FindDescendant("SelectionCheckbox") as CheckBox;
            if (checkbox is not null)
            {
                // Temporarily disable events to avoid selecting wrong items
                checkbox.Checked -= ItemSelected_Checked;
                checkbox.Unchecked -= ItemSelected_Unchecked;

                checkbox.IsChecked = FileList.SelectedItems.Contains(item);

                checkbox.Checked += ItemSelected_Checked;
                checkbox.Unchecked += ItemSelected_Unchecked;
            }
            UpdateCheckboxVisibility(container, checkbox?.IsPointerOver ?? false);
        }
    }

    private void ItemSelected_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.DataContext is ListedItem item && !FileList.SelectedItems.Contains(item))
        {
            FileList.SelectedItems.Add(item);
        }
    }

    private void ItemSelected_Unchecked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.DataContext is ListedItem item && FileList.SelectedItems.Contains(item))
        {
            FileList.SelectedItems.Remove(item);
        }
    }

    private void UpdateCheckboxVisibility(object sender, bool isPointerOver)
    {
        if (sender is ListViewItem control && control.FindDescendant<UserControl>() is UserControl userControl)
        {
            // Handle visual states
            // Show checkboxes when items are selected (as long as the setting is enabled)
            // Show checkboxes when hovering of the thumbnail (regardless of the setting to hide them)
            if (control.IsSelected || isPointerOver)
            {
                VisualStateManager.GoToState(userControl, "ShowCheckbox", true);
            }
            else
            {
                VisualStateManager.GoToState(userControl, "HideCheckbox", true);
            }
        }
    }

    #endregion
}
