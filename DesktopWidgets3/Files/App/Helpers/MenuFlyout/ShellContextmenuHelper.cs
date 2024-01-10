// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.Input;
using DesktopWidgets3.Helpers;
using Files.App.Utils;
using Files.App.Data.Models;
using Files.App.Helpers.ContextFlyouts;
using Files.App.Utils.Shell;
using Files.Core.Data.Enums;
using Files.Core.Data.Items;
using Files.Shared.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Vanara.PInvoke;

namespace Files.App.Helpers;

public static class ShellContextmenuHelper
{
    public static async Task<List<ContextMenuFlyoutItemViewModel>> GetShellContextmenuAsync(
        bool showOpenMenu, 
        bool shiftPressed, 
        string workingDirectory, 
        List<ListedItem>? selectedItems, 
        CancellationToken cancellationToken)
    {
        var IsItemSelected = selectedItems?.Count > 0;

        var menuItemsList = new List<ContextMenuFlyoutItemViewModel>();

        var filePaths = IsItemSelected
            ? selectedItems!.Select(x => x.ItemPath).ToArray()
            : new[] { workingDirectory };

        Func<string, bool> FilterMenuItems(bool showOpenMenu)
        {
            var knownItems = new HashSet<string>()
            {
                "opennew", "opencontaining", "opennewprocess",
                "runas", "runasuser", "pintohome", "PinToStartScreen",
                "cut", "copy", "paste", "delete", "properties", "link",
                "Windows.ModernShare", "Windows.Share", "setdesktopwallpaper",
                "eject", "rename", "explore", "openinfiles", "extract",
                "copyaspath", "undelete", "empty", "format", "rotate90", "rotate270",
                Win32API.ExtractStringFromDLL("shell32.dll", 34593), // Add to collection
				Win32API.ExtractStringFromDLL("shell32.dll", 5384), // Pin to Start
				Win32API.ExtractStringFromDLL("shell32.dll", 5385), // Unpin from Start
				Win32API.ExtractStringFromDLL("shell32.dll", 5386), // Pin to taskbar
				Win32API.ExtractStringFromDLL("shell32.dll", 5387), // Unpin from taskbar
			};

            bool filterMenuItemsImpl(string menuItem) => !string.IsNullOrEmpty(menuItem)
                && (knownItems.Contains(menuItem) || (!showOpenMenu && menuItem.Equals("open", StringComparison.OrdinalIgnoreCase)));

            return filterMenuItemsImpl;
        }

        var contextMenu = await ContextMenu.GetContextMenuForFiles(filePaths,
            shiftPressed ? Shell32.CMF.CMF_EXTENDEDVERBS : Shell32.CMF.CMF_NORMAL, FilterMenuItems(showOpenMenu));

        if (contextMenu is not null)
        {
            LoadMenuFlyoutItem(menuItemsList, contextMenu, contextMenu.Items!, cancellationToken, true);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            menuItemsList.Clear();
        }

        return menuItemsList;
    }

    private static void LoadMenuFlyoutItem(
        IList<ContextMenuFlyoutItemViewModel> menuItemsListLocal,
        ContextMenu contextMenu,
        IEnumerable<Win32ContextMenuItem> menuFlyoutItems,
        CancellationToken cancellationToken,
        bool showIcons = true,
        int itemsBeforeOverflow = int.MaxValue)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        var itemsCount = 0; // Separators do not count for reaching the overflow threshold
        var menuItems = menuFlyoutItems.TakeWhile(x => x.Type == MenuItemType.MFT_SEPARATOR || ++itemsCount <= itemsBeforeOverflow).ToList();
        var overflowItems = menuFlyoutItems.Except(menuItems).ToList();

        if (overflowItems.Where(x => x.Type != MenuItemType.MFT_SEPARATOR).Any())
        {
            var moreItem = menuItemsListLocal.Where(x => x.ID == "ItemOverflow").FirstOrDefault();
            if (moreItem is null)
            {
                var menuLayoutSubItem = new ContextMenuFlyoutItemViewModel()
                {
                    Text = "ShowMoreOptions".GetLocalized(),
                    Glyph = "\xE712",
                };
                LoadMenuFlyoutItem(menuLayoutSubItem.Items, contextMenu, overflowItems, cancellationToken, showIcons);
                menuItemsListLocal.Insert(0, menuLayoutSubItem);
            }
            else
            {
                LoadMenuFlyoutItem(moreItem.Items, contextMenu, overflowItems, cancellationToken, showIcons);
            }
        }

        foreach (var menuFlyoutItem in menuItems
            .SkipWhile(x => x.Type == MenuItemType.MFT_SEPARATOR) // Remove leading separators
            .Reverse()
            .SkipWhile(x => x.Type == MenuItemType.MFT_SEPARATOR)) // Remove trailing separators
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            // Avoid duplicate separators
            if ((menuFlyoutItem.Type == MenuItemType.MFT_SEPARATOR) && (menuItemsListLocal.FirstOrDefault()?.ItemType == ContextMenuFlyoutItemType.Separator))
            {
                continue;
            }

            BitmapImage? image = null;
            if (showIcons && menuFlyoutItem.Icon is { Length: > 0 })
            {
                image = new BitmapImage();
                using var ms = new MemoryStream(menuFlyoutItem.Icon);
                image.SetSourceAsync(ms.AsRandomAccessStream()).AsTask().Wait(10);
            }

            if (menuFlyoutItem.Type is MenuItemType.MFT_SEPARATOR)
            {
                var menuLayoutItem = new ContextMenuFlyoutItemViewModel()
                {
                    ItemType = ContextMenuFlyoutItemType.Separator,
                    Tag = menuFlyoutItem
                };
                menuItemsListLocal.Insert(0, menuLayoutItem);
            }
            else if (!string.IsNullOrEmpty(menuFlyoutItem.Label) && menuFlyoutItem.SubItems is not null)
            {
                if (string.Equals(menuFlyoutItem.Label, Win32API.ExtractStringFromDLL("shell32.dll", 30312)))
                {
                    menuFlyoutItem.CommandString = "sendto";
                }

                var menuLayoutSubItem = new ContextMenuFlyoutItemViewModel()
                {
                    Text = menuFlyoutItem.Label.Replace("&", "", StringComparison.Ordinal),
                    Tag = menuFlyoutItem,
                    BitmapIcon = image!,
                    Items = new List<ContextMenuFlyoutItemViewModel>(),
                };

                if (menuFlyoutItem.SubItems.Any())
                {
                    LoadMenuFlyoutItem(menuLayoutSubItem.Items, contextMenu, menuFlyoutItem.SubItems, cancellationToken, showIcons);
                }
                else
                {
                    menuLayoutSubItem.LoadSubMenuAction = async () =>
                    {
                        if (await contextMenu.LoadSubMenu(menuFlyoutItem.SubItems))
                        {
                            LoadMenuFlyoutItem(menuLayoutSubItem.Items, contextMenu, menuFlyoutItem.SubItems, cancellationToken, showIcons);
                        }
                    };
                }

                menuItemsListLocal.Insert(0, menuLayoutSubItem);
            }
            else if (!string.IsNullOrEmpty(menuFlyoutItem.Label))
            {
                var menuLayoutItem = new ContextMenuFlyoutItemViewModel
                {
                    Text = menuFlyoutItem.Label.Replace("&", "", StringComparison.Ordinal),
                    Tag = menuFlyoutItem,
                    BitmapIcon = image!,
                    Command = new AsyncRelayCommand<object>(x => InvokeShellMenuItemAsync(contextMenu, x)),
                    CommandParameter = menuFlyoutItem
                };
                menuItemsListLocal.Insert(0, menuLayoutItem);
            }
        }

        async Task InvokeShellMenuItemAsync(ContextMenu contextMenu, object? tag)
        {
            if (tag is not Win32ContextMenuItem menuItem)
            {
                return;
            }

            var menuId = menuItem.ID;
            var isFont = FileExtensionHelpers.IsFontFile(contextMenu.ItemsPath[0]);
            var verb = menuItem.CommandString;
            switch (verb)
            {
                case "install" when isFont:
                    {
                        foreach (var path in contextMenu.ItemsPath)
                        {
                            await Win32API.InstallFont(path, false);
                        }
                    }
                    break;

                case "installAllUsers" when isFont:
                    {
                        foreach (var path in contextMenu.ItemsPath)
                        {
                            await Win32API.InstallFont(path, true);
                        }
                    }
                    break;

                case "mount":
                    var vhdPath = contextMenu.ItemsPath[0];
                    await Win32API.MountVhdDisk(vhdPath);
                    break;

                case "format":
                    var drivePath = contextMenu.ItemsPath[0];
                    await Win32API.OpenFormatDriveDialog(drivePath);
                    break;

                default:
                    await contextMenu.InvokeItem(menuId);
                    break;
            }

            //contextMenu.Dispose(); // Prevents some menu items from working (TBC)
        }
    }

    public static List<ContextMenuFlyoutItemViewModel>? GetOpenWithItems(List<ContextMenuFlyoutItemViewModel> flyout)
    {
        var item = flyout.FirstOrDefault(x => x.Tag is Win32ContextMenuItem { CommandString: "openas" });
        if (item is not null)
            flyout.Remove(item);
        return item?.Items;
    }

    public static List<ContextMenuFlyoutItemViewModel>? GetSendToItems(List<ContextMenuFlyoutItemViewModel> flyout)
    {
        var item = flyout.FirstOrDefault(x => x.Tag is Win32ContextMenuItem { CommandString: "sendto" });
        if (item is not null)
            flyout.Remove(item);
        return item?.Items;
    }

    public static void AddItemsToMainMenu(IEnumerable<ICommandBarElement> mainMenu, ContextMenuFlyoutItemViewModel viewModel)
    {
        var appBarButton = mainMenu.FirstOrDefault(x => (x as AppBarButton)?.Tag == viewModel.Tag) as AppBarButton;

        if (appBarButton is not null)
        {
            var ctxFlyout = new MenuFlyout();
            ItemModelListToContextFlyoutHelper.GetMenuFlyoutItemsFromModel(viewModel.Items)?.ForEach(i => ctxFlyout.Items.Add(i));
            appBarButton.Flyout = ctxFlyout;
            appBarButton.Visibility = Visibility.Collapsed;
            appBarButton.Visibility = Visibility.Visible;
        }
    }

    public static void AddItemsToOverflowMenu(AppBarButton? overflowItem, ContextMenuFlyoutItemViewModel viewModel)
    {
        if (overflowItem?.Flyout is MenuFlyout flyout)
        {
            var flyoutSubItem = flyout.Items.FirstOrDefault(x => x.Tag == viewModel.Tag) as MenuFlyoutSubItem;
            if (flyoutSubItem is not null)
            {
                viewModel.Items.ForEach(i => flyoutSubItem.Items.Add(ItemModelListToContextFlyoutHelper.GetMenuItem(i)));
                flyout.Items[flyout.Items.IndexOf(flyoutSubItem) + 1].Visibility = Visibility.Collapsed;
                flyoutSubItem.Visibility = Visibility.Visible;
            }
        }
    }
}
