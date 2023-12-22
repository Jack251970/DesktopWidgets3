using Microsoft.UI.Xaml.Controls;

using DesktopWidgets3.ViewModels.Pages.Widget;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Files.App.Utils;
using Microsoft.UI.Xaml.Media;
using CommunityToolkit.WinUI.UI;
using Files.App.Views.Layouts;
using Microsoft.UI.Input;
using Windows.System;
using Windows.UI.Core;

namespace DesktopWidgets3.Views.Pages.Widget;

public sealed partial class FolderViewPage : BaseLayoutPage
{
    public override FolderViewViewModel ViewModel
    {
        get;
    }

    public FolderViewPage() : base()
    {
        ViewModel = App.GetService<FolderViewViewModel>();
        InitializeComponent();
        Initialize();
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

    #region item open

    private async void FileList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        // Skip opening selected items if the double tap doesn't capture an item
        if (e.OriginalSource is FrameworkElement { DataContext: ListedItem })
        {
            await ViewModel.CommandManager.OpenItem.ExecuteAsync();
        }
        // TODO: Add UserSettingsService.FoldersSettingsService.DoubleClickToGoUp here.
        /*else if (UserSettingsService.FoldersSettingsService.DoubleClickToGoUp)
        {
            await Commands.NavigateUp.ExecuteAsync();
        }*/
        ResetRenameDoubleClick();
    }

    #endregion

    #region abstract

    // Abstract properties
    protected override ItemsControl ItemsControl => FileList;

    // Abstract methods
    protected override bool CanGetItemFromElement(object element) => element is ListViewItem;

    protected override void InitializeItemManipulationModel()
    {
        ItemManipulationModel.ClearSelectionInvoked += (s, e) =>
        {
            FileList.SelectedItems.Clear();
        };
        ItemManipulationModel.AddSelectedItemInvoked += (s, e) =>
        {
            /*if (NextRenameIndex != 0)
            {
                _nextItemToSelect = e;
                FileList.LayoutUpdated += FileList_LayoutUpdated;
            } else */
            if (FileList?.Items.Contains(e) ?? false)
            {
                FileList!.SelectedItems.Add(e);
            }
        };
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

    #region item select

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

    #endregion

    #region item rename

    private async void FileList_ItemTapped(object sender, TappedRoutedEventArgs e)
    {
        var clickedItem = e.OriginalSource as FrameworkElement;
        var ctrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
        var shiftPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
        if (clickedItem?.DataContext is not ListedItem)
        {
            if (IsRenamingItem && RenamingItem is not null)
            {
                var listViewItem = FileList.ContainerFromItem(RenamingItem) as ListViewItem;
                if (listViewItem is not null)
                {
                    var textBox = listViewItem.FindDescendant("ItemNameTextBox") as TextBox;
                    if (textBox is not null)
                    {
                        await CommitRenameAsync(textBox);
                    }
                }
            }
            return;
        }

        // Skip code if the control or shift key is pressed or if the user is using multiselect
        if
        (
            ctrlPressed ||
            shiftPressed ||
            clickedItem is Microsoft.UI.Xaml.Shapes.Rectangle
        )
        {
            e.Handled = true;
            return;
        }

        if (clickedItem is TextBlock block && block.Name == "ItemName")
        {
            CheckRenameDoubleClick(clickedItem.DataContext);
        }
        else if (IsRenamingItem && RenamingItem is not null)
        {
            var listViewItem = FileList.ContainerFromItem(RenamingItem) as ListViewItem;
            if (listViewItem is not null)
            {
                var textBox = listViewItem.FindDescendant("ItemNameTextBox") as TextBox;
                if (textBox is not null)
                {
                    await CommitRenameAsync(textBox);
                }
            }
        }
    }

    public override void StartRenameItem()
    {
        StartRenameItem("ItemNameTextBox");

        if (FileList.ContainerFromItem(RenamingItem) is not ListViewItem listViewItem)
        {
            return;
        }

        if (listViewItem.FindDescendant("ItemNameTextBox") is not TextBox textBox || 
            textBox.FindParent<Grid>() is null)
        {
            return;
        }

        Grid.SetColumnSpan(textBox.FindParent<Grid>(), 8);
    }

    private void ItemNameTextBox_BeforeTextChanging(TextBox textBox, TextBoxBeforeTextChangingEventArgs args)
    {
        if (IsRenamingItem)
        {
            //TODO: Add error message in xmal.
            /*ValidateItemNameInputTextAsync(textBox, args, (showError) =>
            {
                FileNameTeachingTip.Visibility = showError ? Visibility.Visible : Visibility.Collapsed;
                FileNameTeachingTip.IsOpen = showError;
            });*/
        }
    }

    protected override void EndRename(TextBox textBox)
    {
        if (textBox is not null && textBox.FindParent<Grid>() is FrameworkElement parent)
        {
            Grid.SetColumnSpan(parent, 1);
        }

        var listViewItem = FileList.ContainerFromItem(RenamingItem) as ListViewItem;

        if (textBox is null || listViewItem is null)
        {
            // Navigating away, do nothing
        }
        else
        {
            var textBlock = listViewItem.FindDescendant("ItemName") as TextBlock;
            textBox.Visibility = Visibility.Collapsed;
            textBlock!.Visibility = Visibility.Visible;
        }

        // Unsubscribe from events
        if (textBox is not null)
        {
            textBox!.LostFocus -= RenameTextBox_LostFocus;
            textBox.KeyDown -= RenameTextBox_KeyDown;
        }

        //FileNameTeachingTip.IsOpen = false;
        IsRenamingItem = false;

        // Re-focus selected list item
        listViewItem?.Focus(FocusState.Programmatic);
    }

    #endregion

    #region item check box

    private new void FileList_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        var selectionCheckbox = args.ItemContainer.FindDescendant("SelectionCheckbox")!;

        selectionCheckbox.PointerEntered -= SelectionCheckbox_PointerEntered;
        selectionCheckbox.PointerExited -= SelectionCheckbox_PointerExited;
        selectionCheckbox.PointerCanceled -= SelectionCheckbox_PointerCanceled;

        base.FileList_ContainerContentChanging(sender, args);
        SetCheckboxSelectionState(args.Item, args.ItemContainer as ListViewItem);

        selectionCheckbox.PointerEntered += SelectionCheckbox_PointerEntered;
        selectionCheckbox.PointerExited += SelectionCheckbox_PointerExited;
        selectionCheckbox.PointerCanceled += SelectionCheckbox_PointerCanceled;
    }

    private void SelectionCheckbox_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        UpdateCheckboxVisibility((sender as FrameworkElement)!.FindAscendant<ListViewItem>()!, true);
    }

    private void SelectionCheckbox_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        UpdateCheckboxVisibility((sender as FrameworkElement)!.FindAscendant<ListViewItem>()!, false);
    }

    private void SelectionCheckbox_PointerCanceled(object sender, PointerRoutedEventArgs e)
    {
        UpdateCheckboxVisibility((sender as FrameworkElement)!.FindAscendant<ListViewItem>()!, false);
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
