using Microsoft.UI.Xaml.Controls;

using DesktopWidgets3.ViewModels.Pages.Widget;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using DesktopWidgets3.Models.Widget.FolderView;
using Microsoft.UI.Xaml.Media;
using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Input;
using Windows.System;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Controls.Primitives;
using Files.App;
using Files.App.Data.Models;
using Files.App.Helpers;
using Files.App.ViewModels.Layouts;
using Files.App.Helpers.ContextFlyouts;
using Files.Shared.Extensions;
using DispatcherQueueTimer = Microsoft.UI.Dispatching.DispatcherQueueTimer;

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

        tapDebounceTimer = DispatcherQueue.CreateTimer();

        InitilizeItemContextFlyout();
        InitializeItemManipulationModel();
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
            await ViewModel.OpenItem(filePath);
        }
    }

    #endregion

    #region item context menu

    public bool IsItemSelected => ViewModel.IsItemSelected;

    public static CommandBarFlyout? LastOpenedFlyout { get; private set; }

    public ItemManipulationModel ItemManipulationModel => ViewModel.ItemManipulationModel;

    public SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel => ViewModel.SelectedItemsPropertiesViewModel;

    private CancellationTokenSource? shellContextMenuItemCancellationToken;

    private bool shiftPressed;

    private void InitializeItemManipulationModel()
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
            } else */if (FileList?.Items.Contains(e) ?? false)
            {
                FileList!.SelectedItems.Add(e);
            }
        };
    }

    public CurrentInstanceViewModel? InstanceViewModel => ViewModel.InstanceViewModel;

    public BaseLayoutViewModel? CommandsViewModel => ViewModel.CommandsViewModel;

    public CommandBarFlyout ItemContextMenuFlyout
    {
        get; set;
    } = new()
    {
        AlwaysExpanded = true,
        AreOpenCloseAnimationsEnabled = false,
        Placement = FlyoutPlacementMode.Right,
    };

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

    private void InitilizeItemContextFlyout()
    {
        ViewModel.NavigatedTo += (s, e) => RegisterItemContextFlyoutEvents();
        ViewModel.NavigatedFrom += (s, e) => UnregisterItemContextFlyoutEvents();
    }

    private void RegisterItemContextFlyoutEvents()
    {
        ItemContextMenuFlyout.Opened += ItemContextFlyout_Opening;
    }

    private void UnregisterItemContextFlyoutEvents()
    {
        ItemContextMenuFlyout.Opened -= ItemContextFlyout_Opening;
    }

    private async void ItemContextFlyout_Opening(object? sender, object e)
    {
        LastOpenedFlyout = sender as CommandBarFlyout;

        try
        {
            // Workaround for item sometimes not getting selected
            if (!IsItemSelected && sender is CommandBarFlyout { Target: ListViewItem { Content: ListedItem li } })
            {
                ItemManipulationModel.SetSelectedItem(li);
            }

            if (IsItemSelected)
            {
                // Reset menu max height
                if (ItemContextMenuFlyout.GetValue(ContextMenuExtensions.ItemsControlProperty) is ItemsControl itc)
                {
                    itc.MaxHeight = Constants.UI.ContextMenuMaxHeight;
                }

                // Check file extensions
                shellContextMenuItemCancellationToken?.Cancel();
                shellContextMenuItemCancellationToken = new CancellationTokenSource();
                var fileExtensions = SelectedItems!.Select(selectedItem => selectedItem?.FileExtension).ToList();
                SelectedItemsPropertiesViewModel.CheckAllFileExtensions(fileExtensions!);

                // Check if shift is pressed
                shiftPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);

                // Clear menu
                ItemContextMenuFlyout.PrimaryCommands.Clear();
                ItemContextMenuFlyout.SecondaryCommands.Clear();

                // Add items
                var items = ContextFlyoutItemHelper.GetItemContextCommandsWithoutShellItems(
                    currentInstanceViewModel: InstanceViewModel!,
                    selectedItems: SelectedItems!,
                    selectedItemsPropertiesViewModel: SelectedItemsPropertiesViewModel,
                    commandsViewModel: CommandsViewModel!,
                    shiftPressed: shiftPressed,
                    commands: ViewModel.CommandManager);
                var (primaryElements, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(items);
                AddCloseHandler(ItemContextMenuFlyout, primaryElements, secondaryElements);
                primaryElements.ForEach(ItemContextMenuFlyout.PrimaryCommands.Add);
                secondaryElements.OfType<FrameworkElement>().ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth); // Set menu min width
                secondaryElements.ForEach(ItemContextMenuFlyout.SecondaryCommands.Add);

                // Add shell menu items
                /*if (!InstanceViewModel.IsPageTypeZipFolder && !InstanceViewModel.IsPageTypeFtp)
                {
                    var shellMenuItems = await ContextFlyoutItemHelper.GetItemContextShellCommandsAsync(
                        workingDir: ParentShellPageInstance.FilesystemViewModel.WorkingDirectory,
                        selectedItems: SelectedItems!, shiftPressed: shiftPressed,
                        showOpenMenu: false, shellContextMenuItemCancellationToken.Token);
                    if (shellMenuItems.Any())
                    {
                        await AddShellMenuItemsAsync(shellMenuItems, ItemContextMenuFlyout, shiftPressed);
                    }
                    else
                    {
                        RemoveOverflow(ItemContextMenuFlyout);
                    }
                }
                else
                {
                    RemoveOverflow(ItemContextMenuFlyout);
                }*/
            }
        }
        catch (Exception)
        {

        }
    }

    private void AddCloseHandler(CommandBarFlyout flyout, IList<ICommandBarElement> primaryElements, IList<ICommandBarElement> secondaryElements)
    {
        // Workaround for WinUI (#5508)
        var closeHandler = new RoutedEventHandler((s, e) => flyout.Hide());

        primaryElements
            .OfType<AppBarButton>()
            .ForEach(button => button.Click += closeHandler);

        var menuFlyoutItems = secondaryElements
            .OfType<AppBarButton>()
            .Select(item => item.Flyout)
            .OfType<MenuFlyout>()
            .SelectMany(menu => menu.Items);

        addCloseHandler(menuFlyoutItems);

        void addCloseHandler(IEnumerable<MenuFlyoutItemBase> menuFlyoutItems)
        {
            menuFlyoutItems.OfType<MenuFlyoutItem>()
                .ForEach(button => button.Click += closeHandler);
            menuFlyoutItems.OfType<MenuFlyoutSubItem>()
                .ForEach(menu => addCloseHandler(menu.Items));
        }
    }


    #endregion

    #region item select

    private List<ListedItem> SelectedItems
    {
        get => ViewModel.SelectedItems;
        set => ViewModel.SelectedItems = value;
    }

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

    private readonly DispatcherQueueTimer tapDebounceTimer;

    private ListedItem? preRenamingItem = null;

    private void FileList_Tapped(object sender, TappedRoutedEventArgs e)
    {
        /*var clickedItem = e.OriginalSource as FrameworkElement;
        var ctrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
        var shiftPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
        if (clickedItem?.DataContext is not ListedItem item)
        {
            if (IsRenamingItem && RenamingItem is not null)
            {
                ListViewItem? listViewItem = FileList.ContainerFromItem(RenamingItem) as ListViewItem;
                if (listViewItem is not null)
                {
                    var textBox = listViewItem.FindDescendant("ItemNameTextBox") as TextBox;
                    if (textBox is not null)
                        await CommitRenameAsync(textBox);
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

        // Handle tapped event to select item
        if (clickedItem is TextBlock block && block.Name == "ItemName")
        {
            CheckRenameDoubleClick(clickedItem.DataContext);
        }
        else if (IsRenamingItem && RenamingItem is not null)
        {
            ListViewItem? listViewItem = FileList.ContainerFromItem(RenamingItem) as ListViewItem;
            if (listViewItem is not null)
            {
                var textBox = listViewItem.FindDescendant("ItemNameTextBox") as TextBox;
                if (textBox is not null)
                    await CommitRenameAsync(textBox);
            }
        }*/
    }

    /*public void CheckRenameDoubleClick(object clickedItem)
    {
        if (clickedItem is ListedItem item)
        {
            if (item == preRenamingItem)
            {
                tapDebounceTimer.Debounce(() =>
                {
                    if (item == preRenamingItem)
                    {
                        StartRenameItem();
                        tapDebounceTimer.Stop();
                    }
                },
                TimeSpan.FromMilliseconds(500));
            }
            else
            {
                tapDebounceTimer.Stop();
                preRenamingItem = item;
            }
        }
        else
        {
            ResetRenameDoubleClick();
        }
    }

    public void StartRenameItem()
    {
        StartRenameItem("ItemNameTextBox");

        if (FileList.ContainerFromItem(RenamingItem) is not ListViewItem listViewItem)
        {
            return;
        }

        var textBox = listViewItem.FindDescendant("ItemNameTextBox") as TextBox;
        if (textBox is null || textBox.FindParent<Grid>() is null)
            return;

        Grid.SetColumnSpan(textBox.FindParent<Grid>(), 8);
    }

    public void ResetRenameDoubleClick()
    {
        preRenamingItem = null;
        tapDebounceTimer.Stop();
    }*/

    #endregion

    #region item check box

    private void FileList_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        var selectionCheckbox = args.ItemContainer.FindDescendant("SelectionCheckbox")!;

        selectionCheckbox.PointerEntered -= SelectionCheckbox_PointerEntered;
        selectionCheckbox.PointerExited -= SelectionCheckbox_PointerExited;
        selectionCheckbox.PointerCanceled -= SelectionCheckbox_PointerCanceled;

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

    public class ContextMenuExtensions : DependencyObject
    {
        public static ItemsControl GetItemsControl(DependencyObject obj)
        {
            return (ItemsControl)obj.GetValue(ItemsControlProperty);
        }

        public static void SetItemsControl(DependencyObject obj, ItemsControl value)
        {
            obj.SetValue(ItemsControlProperty, value);
        }

        public static readonly DependencyProperty ItemsControlProperty =
            DependencyProperty.RegisterAttached("ItemsControl", typeof(ItemsControl), typeof(ContextMenuExtensions), new PropertyMetadata(null));
    }
}
