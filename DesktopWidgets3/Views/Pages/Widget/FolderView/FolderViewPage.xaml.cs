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
using DesktopWidgets3.Views.Windows;
using Files.Core.Data.Enums;
using Files.Core.Data.Items;
using Windows.Foundation;
using DesktopWidgets3.Helpers;

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
                    viewModel: ViewModel,
                    commands: ViewModel.CommandManager);
                var (primaryElements, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(items);
                AddCloseHandler(ItemContextMenuFlyout, primaryElements, secondaryElements);
                primaryElements.ForEach(ItemContextMenuFlyout.PrimaryCommands.Add);
                secondaryElements.OfType<FrameworkElement>().ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth); // Set menu min width
                secondaryElements.ForEach(ItemContextMenuFlyout.SecondaryCommands.Add);

                // Add shell menu items
                if (!InstanceViewModel!.IsPageTypeZipFolder && !InstanceViewModel.IsPageTypeFtp)
                {
                    var shellMenuItems = await ContextFlyoutItemHelper.GetItemContextShellCommandsAsync(
                        workingDir: ViewModel.WorkingDirectory,
                        selectedItems: SelectedItems!, 
                        shiftPressed: shiftPressed,
                        showOpenMenu: false, 
                        shellContextMenuItemCancellationToken.Token);
                    if (shellMenuItems.Any())
                    {
                        await AddShellMenuItemsAsync(ViewModel, shellMenuItems, ItemContextMenuFlyout, shiftPressed);
                    }
                    else
                    {
                        RemoveOverflow(ItemContextMenuFlyout);
                    }
                }
                else
                {
                    RemoveOverflow(ItemContextMenuFlyout);
                }
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

    private async Task AddShellMenuItemsAsync(FolderViewViewModel viewModel, List<ContextMenuFlyoutItemViewModel> shellMenuItems, CommandBarFlyout contextMenuFlyout, bool shiftPressed)
    {
        // TODO: add UserSettingsService.GeneralSettingsService.MoveShellExtensionsToSubMenu into settings
        var moveShellExtensionsToSubMenu = true;
        // TODO: UserSettingsService.GeneralSettingsService.ShowSendToMenu.
        var showSendToMenu = false;
        var mainWindowIntance = viewModel.WidgetWindow;

        var openWithMenuItem = shellMenuItems.FirstOrDefault(x => x.Tag is Win32ContextMenuItem { CommandString: "openas" });
        var sendToMenuItem = shellMenuItems.FirstOrDefault(x => x.Tag is Win32ContextMenuItem { CommandString: "sendto" });
        var turnOnBitLockerMenuItem = shellMenuItems.FirstOrDefault(x => x.Tag is Win32ContextMenuItem menuItem && menuItem.CommandString is not null && menuItem.CommandString.StartsWith("encrypt-bde"));
        var manageBitLockerMenuItem = shellMenuItems.FirstOrDefault(x => x.Tag is Win32ContextMenuItem { CommandString: "manage-bde" });
        var shellMenuItemsFiltered = shellMenuItems.Where(x => x != openWithMenuItem && x != sendToMenuItem && x != turnOnBitLockerMenuItem && x != manageBitLockerMenuItem).ToList();
        var mainShellMenuItems = shellMenuItemsFiltered.RemoveFrom(!moveShellExtensionsToSubMenu ? int.MaxValue : shiftPressed ? 6 : 0);
        var overflowShellMenuItemsUnfiltered = shellMenuItemsFiltered.Except(mainShellMenuItems).ToList();
        var overflowShellMenuItems = overflowShellMenuItemsUnfiltered.Where(
            (x, i) => (x.ItemType == ContextMenuFlyoutItemType.Separator &&
            overflowShellMenuItemsUnfiltered[i + 1 < overflowShellMenuItemsUnfiltered.Count ? i + 1 : i].ItemType != ContextMenuFlyoutItemType.Separator)
            || x.ItemType != ContextMenuFlyoutItemType.Separator).ToList();

        var overflowItems = ItemModelListToContextFlyoutHelper.GetMenuFlyoutItemsFromModel(overflowShellMenuItems);
        var mainItems = ItemModelListToContextFlyoutHelper.GetAppBarButtonsFromModelIgnorePrimary(mainShellMenuItems);

        var openedPopups = VisualTreeHelper.GetOpenPopups(mainWindowIntance);
        var secondaryMenu = openedPopups.FirstOrDefault(popup => popup.Name == "OverflowPopup");

        var itemsControl = secondaryMenu?.Child.FindDescendant<ItemsControl>();
        if (itemsControl is not null && secondaryMenu is not null)
        {
            contextMenuFlyout.SetValue(ContextMenuExtensions.ItemsControlProperty, itemsControl);

            var ttv = secondaryMenu.TransformToVisual(mainWindowIntance.Content);
            var cMenuPos = ttv.TransformPoint(new Point(0, 0));

            var requiredHeight = contextMenuFlyout.SecondaryCommands.Concat(mainItems).Where(x => x is not AppBarSeparator).Count() * Constants.UI.ContextMenuSecondaryItemsHeight;
            var availableHeight = mainWindowIntance.Bounds.Height - cMenuPos.Y - Constants.UI.ContextMenuPrimaryItemsHeight;

            // Set menu max height to current height (Avoid menu repositioning)
            if (requiredHeight > availableHeight)
            {
                itemsControl.MaxHeight = Math.Min(Constants.UI.ContextMenuMaxHeight, Math.Max(itemsControl.ActualHeight, Math.Min(availableHeight, requiredHeight)));
            }

            // Set items max width to current menu width (#5555)
            mainItems.OfType<FrameworkElement>().ForEach(x => x.MaxWidth = itemsControl.ActualWidth - Constants.UI.ContextMenuLabelMargin);
        }

        ContextFlyoutItemHelper.SwapPlaceholderWithShellOption(
            contextMenuFlyout,
            "TurnOnBitLockerPlaceholder",
            turnOnBitLockerMenuItem,
            contextMenuFlyout.SecondaryCommands.Count - 2
        );
        ContextFlyoutItemHelper.SwapPlaceholderWithShellOption(
            contextMenuFlyout,
            "ManageBitLockerPlaceholder",
            manageBitLockerMenuItem,
            contextMenuFlyout.SecondaryCommands.Count - 2
        );

        var overflowItem = contextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarButton appBarButton && (appBarButton.Tag as string) == "ItemOverflow") as AppBarButton;
        if (overflowItem is not null)
        {
            var overflowItemFlyout = overflowItem.Flyout as MenuFlyout;
            if (overflowItemFlyout is not null)
            {
                if (overflowItemFlyout.Items.Count > 0)
                {
                    overflowItemFlyout.Items.Insert(0, new MenuFlyoutSeparator());
                }

                var index = contextMenuFlyout.SecondaryCommands.Count - 2;
                foreach (var i in mainItems)
                {
                    index++;
                    contextMenuFlyout.SecondaryCommands.Insert(index, i);
                }

                index = 0;
                foreach (var i in overflowItems!)
                {
                    overflowItemFlyout.Items.Insert(index, i);
                    index++;
                }

                if (overflowItemFlyout.Items.Count > 0 && moveShellExtensionsToSubMenu)
                {
                    overflowItem.Label = "ShowMoreOptions".GetLocalized();
                    overflowItem.IsEnabled = true;
                }
                else
                {
                    overflowItem.Visibility = Visibility.Collapsed;

                    // Hide separators at the end of the menu
                    while (contextMenuFlyout.SecondaryCommands.LastOrDefault(x => x is UIElement element && element.Visibility is Visibility.Visible) is AppBarSeparator separator)
                    {
                        separator.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }
        else
        {
            mainItems.ForEach(contextMenuFlyout.SecondaryCommands.Add);
        }

        // Add items to openwith dropdown
        var openWithOverflow = contextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarButton abb && (abb.Tag as string) == "OpenWithOverflow") as AppBarButton;

        var openWith = contextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarButton abb && (abb.Tag as string) == "OpenWith") as AppBarButton;
        if (openWithMenuItem?.LoadSubMenuAction is not null && openWithOverflow is not null && openWith is not null)
        {
            await openWithMenuItem.LoadSubMenuAction();
            var openWithSubItems = ItemModelListToContextFlyoutHelper.GetMenuFlyoutItemsFromModel(
                ShellContextmenuHelper.GetOpenWithItems(shellMenuItems));

            if (openWithSubItems is not null)
            {
                var flyout = (MenuFlyout)openWithOverflow.Flyout;

                flyout.Items.Clear();

                foreach (var item in openWithSubItems)
                {
                    flyout.Items.Add(item);
                }

                openWithOverflow.Flyout = flyout;
                openWith.Visibility = Visibility.Collapsed;
                openWithOverflow.Visibility = Visibility.Visible;
            }
        }

        // Add items to sendto dropdown
        if (showSendToMenu)
        {
            var sendToOverflow = contextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarButton abb && (abb.Tag as string) == "SendToOverflow") as AppBarButton;

            var sendTo = contextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarButton abb && (abb.Tag as string) == "SendTo") as AppBarButton;
            if (sendToMenuItem?.LoadSubMenuAction is not null && sendToOverflow is not null && sendTo is not null)
            {
                await sendToMenuItem.LoadSubMenuAction();
                var sendToSubItems = ItemModelListToContextFlyoutHelper.GetMenuFlyoutItemsFromModel(
                    ShellContextmenuHelper.GetSendToItems(shellMenuItems));

                if (sendToSubItems is not null)
                {
                    var flyout = (MenuFlyout)sendToOverflow.Flyout;

                    flyout.Items.Clear();

                    foreach (var item in sendToSubItems)
                    {
                        flyout.Items.Add(item);
                    }

                    sendToOverflow.Flyout = flyout;
                    sendTo.Visibility = Visibility.Collapsed;
                    sendToOverflow.Visibility = Visibility.Visible;
                }
            }
        }

        // Add items to main shell submenu
        mainShellMenuItems.Where(x => x.LoadSubMenuAction is not null).ForEach(async x =>
        {
            await x.LoadSubMenuAction();

            ShellContextmenuHelper.AddItemsToMainMenu(mainItems, x);
        });

        // Add items to overflow shell submenu
        overflowShellMenuItems.Where(x => x.LoadSubMenuAction is not null).ForEach(async x =>
        {
            await x.LoadSubMenuAction();

            ShellContextmenuHelper.AddItemsToOverflowMenu(overflowItem, x);
        });

        itemsControl?.Items.OfType<FrameworkElement>().ForEach(item =>
        {
            // Enable CharacterEllipsis text trimming for menu items
            if (item.FindDescendant("OverflowTextLabel") is TextBlock label)
            {
                label.TextTrimming = TextTrimming.CharacterEllipsis;
            }

            // Close main menu when clicking on subitems (#5508)
            if ((item as AppBarButton)?.Flyout as MenuFlyout is MenuFlyout flyout)
            {
                Action<IList<MenuFlyoutItemBase>> clickAction = null!;
                clickAction = (items) =>
                {
                    items.OfType<MenuFlyoutItem>().ForEach(i =>
                    {
                        i.Click += new RoutedEventHandler((s, e) => contextMenuFlyout.Hide());
                    });
                    items.OfType<MenuFlyoutSubItem>().ForEach(i =>
                    {
                        clickAction(i.Items);
                    });
                };

                clickAction(flyout.Items);
            }
        });
    }

    private void RemoveOverflow(CommandBarFlyout contextMenuFlyout)
    {
        var overflowItem = contextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarButton appBarButton && (appBarButton.Tag as string) == "ItemOverflow") as AppBarButton;
        var overflowSeparator = contextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarSeparator appBarSeparator && (appBarSeparator.Tag as string) == "OverflowSeparator") as AppBarSeparator;

        if (overflowItem is not null)
        {
            overflowItem.Visibility = Visibility.Collapsed;
        }

        if (overflowSeparator is not null)
        {
            overflowSeparator.Visibility = Visibility.Collapsed;
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
