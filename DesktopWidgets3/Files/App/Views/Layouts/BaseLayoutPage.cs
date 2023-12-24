// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Utils;
using DesktopWidgets3.ViewModels.Pages.Widget;
using Files.App.Data.Models;
using Files.App.Helpers.ContextFlyouts;
using Files.App.Helpers;
using Files.Core.Data.Enums;
using Files.Core.Data.Items;
using Files.Shared.Extensions;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;
using Windows.UI.Core;
using DesktopWidgets3.Helpers;
using Windows.Foundation;
using Files.App.Data.Commands;
using Files.App.ViewModels.Layouts;
using CommunityToolkit.WinUI.UI;
using Files.App.Utils.Storage;
using Files.App.Extensions;
using Microsoft.UI.Xaml.Data;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DispatcherQueueTimer = Microsoft.UI.Dispatching.DispatcherQueueTimer;

namespace Files.App.Views.Layouts;

/// <summary>
/// Represents the base class which every layout page must derive from
/// </summary>
public abstract class BaseLayoutPage : Page, INotifyPropertyChanged
{
    #region Abstract

    // Abstract properties
    public abstract FolderViewViewModel ViewModel { get; }
    protected abstract ItemsControl ItemsControl { get; }
    protected abstract uint IconSize { get; }

    // Abstract methods
    protected abstract bool CanGetItemFromElement(object element);
    protected abstract void InitializeItemManipulationModel();
    protected abstract void EndRename(TextBox textBox);

    #endregion

    #region Properties

    // ViewModel properties
    protected bool IsItemSelected => ViewModel.IsItemSelected;
    protected ItemManipulationModel ItemManipulationModel => ViewModel.ItemManipulationModel;
    protected CurrentInstanceViewModel InstanceViewModel => ViewModel.InstanceViewModel;
    protected SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel => ViewModel.SelectedItemsPropertiesViewModel;
    protected BaseLayoutViewModel CommandsViewModel => ViewModel.CommandsViewModel;
    protected ICommandManager CommandsManager => ViewModel.CommandManager;
    protected ListedItem? SelectedItem => ViewModel.SelectedItem;
    protected List<ListedItem> SelectedItems
    {
        get => ViewModel.SelectedItems;
        set => ViewModel.SelectedItems = value;
    }
    protected bool IsRenamingItem
    {
        get => ViewModel.IsRenamingItem;
        set => ViewModel.IsRenamingItem = value;
    }
    protected ListedItem? RenamingItem
    {
        get => ViewModel.RenamingItem;
        set => ViewModel.RenamingItem = value;
    }
    protected string? OldItemName
    {
        get => ViewModel.OldItemName;
        set => ViewModel.OldItemName = value;
    }

    // Context menu properties
    public static CommandBarFlyout? LastOpenedFlyout { get; private set; }
    public CommandBarFlyout ItemContextMenuFlyout { get; set; } = new()
    {
        AlwaysExpanded = true,
        AreOpenCloseAnimationsEnabled = false,
        Placement = FlyoutPlacementMode.Right,
    };
    private CancellationTokenSource? shellContextMenuItemCancellationToken;
    private bool shiftPressed;

    // Item rename properties
    private readonly DispatcherQueueTimer tapDebounceTimer;
    private ListedItem? preRenamingItem = null;
    private const int KEY_DOWN_MASK = 0x8000;
    protected int NextRenameIndex = 0;

    #endregion

    #region Methods

    // Initialize after the ViewModel is not null.
    protected void Initialize()
    {
        InitilizeItemContextFlyout();
        InitializeItemManipulationModel();
    }

    protected ListedItem? GetItemFromElement(object element)
    {
        return element is not ContentControl item || !CanGetItemFromElement(element)
            ? null
            : (item.DataContext as ListedItem) ?? (item.Content as ListedItem) ?? (ItemsControl.ItemFromContainer(item) as ListedItem);
    }

    #endregion

    public BaseLayoutPage()
    {
        tapDebounceTimer = DispatcherQueue.CreateTimer();
    }

    #region Item context menu

    private void InitilizeItemContextFlyout()
    {
        ViewModel.NavigatedTo += (s, e) => {
            ItemContextMenuFlyout.Opened -= ItemContextFlyout_Opening;
            ItemContextMenuFlyout.Opened += ItemContextFlyout_Opening;
        };
        ViewModel.NavigatedFrom += (s, e) => { 
            ItemContextMenuFlyout.Opened -= ItemContextFlyout_Opening; 
        };
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
                var fileExtensions = SelectedItems.Select(selectedItem => selectedItem?.FileExtension).ToList();
                SelectedItemsPropertiesViewModel.CheckAllFileExtensions(fileExtensions!);

                // Check if shift is pressed
                shiftPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);

                // Clear menu
                ItemContextMenuFlyout.PrimaryCommands.Clear();
                ItemContextMenuFlyout.SecondaryCommands.Clear();

                // Add items
                var items = ContextFlyoutItemHelper.GetItemContextCommandsWithoutShellItems(
                    currentInstanceViewModel: InstanceViewModel,
                    selectedItems: SelectedItems,
                    selectedItemsPropertiesViewModel: SelectedItemsPropertiesViewModel,
                    commandsViewModel: CommandsViewModel,
                    shiftPressed: shiftPressed,
                    viewModel: ViewModel,
                    commands: CommandsManager);
                var (primaryElements, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(items);
                AddCloseHandler(ItemContextMenuFlyout, primaryElements, secondaryElements);
                primaryElements.ForEach(ItemContextMenuFlyout.PrimaryCommands.Add);
                secondaryElements.OfType<FrameworkElement>().ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth); // Set menu min width
                secondaryElements.ForEach(ItemContextMenuFlyout.SecondaryCommands.Add);

                // Add shell menu items
                if (!InstanceViewModel.IsPageTypeZipFolder && !InstanceViewModel.IsPageTypeFtp)
                {
                    var shellMenuItems = await ContextFlyoutItemHelper.GetItemContextShellCommandsAsync(
                        workingDir: ViewModel.ItemViewModel.WorkingDirectory,
                        selectedItems: SelectedItems,
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

    #region Item rename

    protected async virtual Task CommitRenameAsync(TextBox textBox)
    {
        EndRename(textBox);
        var newItemName = textBox.Text.Trim().TrimEnd('.');

        await UIFileSystemHelpers.RenameFileItemAsync(ViewModel, RenamingItem!, newItemName);
    }

    public void CheckRenameDoubleClick(object clickedItem)
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

    public virtual void StartRenameItem()
    {
    }

    protected virtual void StartRenameItem(string itemNameTextBox)
    {
        RenamingItem = SelectedItem;
        if (RenamingItem is null)
        {
            return;
        }

        if (ItemsControl.ContainerFromItem(RenamingItem) is not ListViewItem listViewItem)
        {
            return;
        }

        var textBlock = listViewItem.FindDescendant("ItemName") as TextBlock;
        var textBox = listViewItem.FindDescendant(itemNameTextBox) as TextBox;
        textBox!.Text = textBlock!.Text;
        OldItemName = textBlock.Text;
        textBlock.Visibility = Visibility.Collapsed;
        textBox.Visibility = Visibility.Visible;

        if (textBox.FindParent<Grid>() is null)
        {
            textBlock.Visibility = Visibility.Visible;
            textBox.Visibility = Visibility.Collapsed;
            return;
        }

        Grid.SetColumnSpan(textBox.FindParent<Grid>(), 8);

        textBox.Focus(FocusState.Pointer);
        textBox.LostFocus += RenameTextBox_LostFocus;
        textBox.KeyDown += RenameTextBox_KeyDown;

        var selectedTextLength = SelectedItem!.Name.Length;

        if (!SelectedItem.IsShortcut && ViewModel.GetSettings().ShowExtension)
        {
            var extensionLength = RenamingItem.FileExtension?.Length ?? 0;
            selectedTextLength -= extensionLength;
        }

        textBox.Select(0, selectedTextLength);
        IsRenamingItem = true;
    }

    protected async virtual void RenameTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        var mainWindowInstance = ViewModel.WidgetWindow;
        // This check allows the user to use the text box context menu without ending the rename
        if (!(FocusManager.GetFocusedElement(mainWindowInstance.Content.XamlRoot) is AppBarButton or Popup))
        {
            var textBox = (TextBox)e.OriginalSource;
            await CommitRenameAsync(textBox);
        }
    }

    protected async void RenameTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        var textBox = (TextBox)sender;
        var listViewBase = ItemsControl as ListViewBase;
        switch (e.Key)
        {
            case VirtualKey.Escape:
                textBox.LostFocus -= RenameTextBox_LostFocus;
                textBox.Text = OldItemName;
                EndRename(textBox);
                e.Handled = true;
                break;
            case VirtualKey.Enter:
                textBox.LostFocus -= RenameTextBox_LostFocus;
                await CommitRenameAsync(textBox);
                e.Handled = true;
                break;
            case VirtualKey.Up:
                textBox.SelectionStart = 0;
                e.Handled = true;
                break;
            case VirtualKey.Down:
                textBox.SelectionStart = textBox.Text.Length;
                e.Handled = true;
                break;
            case VirtualKey.Left:
                e.Handled = textBox.SelectionStart == 0;
                break;
            case VirtualKey.Right:
                e.Handled = (textBox.SelectionStart + textBox.SelectionLength) == textBox.Text.Length;
                break;
            case VirtualKey.Tab:
                textBox.LostFocus -= RenameTextBox_LostFocus;

                var isShiftPressed = (InteropHelpers.GetKeyState((int)VirtualKey.Shift) & KEY_DOWN_MASK) != 0;
                NextRenameIndex = isShiftPressed ? -1 : 1;

                if (textBox.Text != OldItemName)
                {
                    await CommitRenameAsync(textBox);
                }
                else
                {
                    var newIndex = listViewBase!.SelectedIndex + NextRenameIndex;
                    NextRenameIndex = 0;
                    EndRename(textBox);

                    if (newIndex >= 0 &&
                        newIndex < listViewBase.Items.Count)
                    {
                        listViewBase.SelectedIndex = newIndex;
                        StartRenameItem();
                    }
                }

                e.Handled = true;
                break;
        }
    }

    public void ResetRenameDoubleClick()
    {
        preRenamingItem = null;
        tapDebounceTimer.Stop();
    }

    protected async Task ValidateItemNameInputTextAsync(TextBox textBox, TextBoxBeforeTextChangingEventArgs args, Action<bool> showError)
    {
        if (FileSystemHelpers.ContainsRestrictedCharacters(args.NewText))
        {
            args.Cancel = true;

            await DispatcherQueue.EnqueueOrInvokeAsync(() =>
            {
                var oldSelection = textBox.SelectionStart + textBox.SelectionLength;
                var oldText = textBox.Text;
                textBox.Text = FileSystemHelpers.FilterRestrictedCharacters(args.NewText);
                textBox.SelectionStart = oldSelection + textBox.Text.Length - oldText.Length;
                showError?.Invoke(true);
            });
        }
        else
        {
            showError?.Invoke(false);
        }
    }

    #endregion

    #region Container Content

    protected void FileList_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        RefreshContainer(args.ItemContainer, args.InRecycleQueue);
        RefreshItem(args.ItemContainer, args.Item, args.InRecycleQueue, args);
    }

    private void RefreshContainer(SelectorItem container, bool inRecycleQueue)
    {
        container.PointerPressed -= FileListItem_PointerPressed;
        container.RightTapped -= FileListItem_RightTapped;

        if (inRecycleQueue)
        {
            //UninitializeDrag(container);
        }
        else
        {
            container.PointerPressed += FileListItem_PointerPressed;
            container.RightTapped += FileListItem_RightTapped;
        }
    }

    protected static void FileListItem_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not SelectorItem selectorItem)
        {
            return;
        }

        if (selectorItem.IsSelected && e.KeyModifiers == VirtualKeyModifiers.Control)
        {
            selectorItem.IsSelected = false;

            // Prevent issues arising caused by the default handlers attempting to select the item that has just been deselected by ctrl + click
            e.Handled = true;
        }
        else if (!selectorItem.IsSelected && e.GetCurrentPoint(selectorItem).Properties.IsLeftButtonPressed)
        {
            selectorItem.IsSelected = true;
        }
    }

    protected void FileListItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        var rightClickedItem = GetItemFromElement(sender);

        if (rightClickedItem is not null && !((SelectorItem)sender).IsSelected)
        {
            ItemManipulationModel.SetSelectedItem(rightClickedItem);
        }
    }

    private void RefreshItem(SelectorItem container, object item, bool inRecycleQueue, ContainerContentChangingEventArgs args)
    {
        if (item is not ListedItem listedItem)
        {
            return;
        }

        if (inRecycleQueue)
        {
            ViewModel.ItemViewModel.CancelExtendedPropertiesLoadingForItem(listedItem);
        }
        else
        {
            //InitializeDrag(container, listedItem);

            if (!listedItem.ItemPropertiesInitialized)
            {
                uint callbackPhase = 3;
                args.RegisterUpdateCallback(callbackPhase, async (s, c) =>
                {
                    await ViewModel.ItemViewModel.LoadExtendedItemPropertiesAsync(listedItem, IconSize);
                    /*if (ViewModel.ItemViewModel.EnabledGitProperties is not GitProperties.None && listedItem is GitItem gitItem)
                    {
                        await ViewModel.ItemViewModel.LoadGitPropertiesAsync(gitItem);
                    }*/
                });
            }
        }
    }

    #endregion

    #region Notify property changed

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
