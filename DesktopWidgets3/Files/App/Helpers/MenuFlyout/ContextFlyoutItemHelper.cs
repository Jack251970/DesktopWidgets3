﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.Helpers;
using Files.App.Utils;
using DesktopWidgets3.ViewModels.Pages.Widget;
using Files.App.Data.Commands;
using Files.App.Data.Models;
using Files.App.Helpers.ContextFlyouts;
using Files.App.ViewModels.Layouts;
using Files.Core.Data.Enums;
using Files.Shared.Helpers;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using Files.Core.Services;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Files.App.Helpers;

/// <summary>
/// Used to create lists of ContextMenuFlyoutItemViewModels that can be used by ItemModelListToContextFlyoutHelper to create context
/// menus and toolbars for the user.
/// <see cref="ContextMenuFlyoutItemViewModel"/>
/// <see cref="ContextFlyouts.ItemModelListToContextFlyoutHelper"/>
/// </summary>
public static class ContextFlyoutItemHelper
{
    private static readonly IAddItemService addItemService = DesktopWidgets3.App.GetService<IAddItemService>();

    #region base menu items

    public static List<ContextMenuFlyoutItemViewModel> GetItemContextCommandsWithoutShellItems(
        CurrentInstanceViewModel currentInstanceViewModel, 
        List<ListedItem> selectedItems, 
        BaseLayoutViewModel commandsViewModel, 
        bool shiftPressed, 
        SelectedItemsPropertiesViewModel? selectedItemsPropertiesViewModel,
        ItemViewModel? itemViewModel,
        FolderViewViewModel viewModel,
        ICommandManager commands)
    {
        var menuItemsList = GetBaseItemMenuItems(
            commandsViewModel: commandsViewModel, 
            selectedItems: selectedItems, 
            selectedItemsPropertiesViewModel: selectedItemsPropertiesViewModel, 
            currentInstanceViewModel: currentInstanceViewModel,
            itemViewModel: itemViewModel,
            viewModel: viewModel,
            commands: commands);
        menuItemsList = Filter(
            items: menuItemsList, 
            shiftPressed: shiftPressed, 
            currentInstanceViewModel: currentInstanceViewModel, 
            selectedItems: selectedItems, 
            removeOverflowMenu: false);
        return menuItemsList;
    }

    public static List<ContextMenuFlyoutItemViewModel> GetBaseItemMenuItems(
            BaseLayoutViewModel commandsViewModel,
            SelectedItemsPropertiesViewModel? selectedItemsPropertiesViewModel,
            List<ListedItem> selectedItems,
            CurrentInstanceViewModel currentInstanceViewModel,
            ItemViewModel? itemViewModel,
            FolderViewViewModel viewModel,
            ICommandManager commands)
    {
        var itemsSelected = itemViewModel is null;
        var canDecompress = selectedItems.Any() && selectedItems.All(x => x.IsArchive)
            || selectedItems.All(x => x.PrimaryItemAttribute == StorageItemTypes.File && FileExtensionHelpers.IsZipFile(x.FileExtension));
        var canCompress = !canDecompress || selectedItems.Count > 1;
        var showOpenItemWith = selectedItems.All(
            i => (i.PrimaryItemAttribute == StorageItemTypes.File && !i.IsShortcut && !i.IsExecutable) || (i.PrimaryItemAttribute == StorageItemTypes.Folder && i.IsArchive));
        var areAllItemsFolders = selectedItems.All(i => i.PrimaryItemAttribute == StorageItemTypes.Folder);
        var isFirstFileExecutable = FileExtensionHelpers.IsExecutableFile(selectedItems.FirstOrDefault()?.FileExtension);
        var newArchiveName =
            Path.GetFileName(selectedItems.Count is 1 ? selectedItems[0].ItemPath : Path.GetDirectoryName(selectedItems[0].ItemPath))
            ?? string.Empty;

        var isDriveRoot = itemViewModel?.CurrentFolder is not null && (itemViewModel.CurrentFolder.ItemPath == Path.GetPathRoot(itemViewModel.CurrentFolder.ItemPath));

        var list =  new List<ContextMenuFlyoutItemViewModel>()
        {
            /*new()
            {
                Text = "LayoutMode".GetLocalizedResource(),
                Glyph = "\uE152",
                ShowItem = !itemsSelected,
                ShowInRecycleBin = true,
                ShowInSearchPage = true,
                ShowInFtpPage = true,
                ShowInZipPage = true,
                Items = new List<ContextMenuFlyoutItemViewModel>
                {
                    new ContextMenuFlyoutItemViewModelBuilder(commands.LayoutDetails)
                    {
                        IsToggle = true
                    }.Build(),
                    new ContextMenuFlyoutItemViewModelBuilder(commands.LayoutTiles)
                    {
                        IsToggle = true
                    }.Build(),
                    new ContextMenuFlyoutItemViewModelBuilder(commands.LayoutGridSmall)
                    {
                        IsToggle = true
                    }.Build(),
                    new ContextMenuFlyoutItemViewModelBuilder(commands.LayoutGridMedium)
                    {
                        IsToggle = true
                    }.Build(),
                    new ContextMenuFlyoutItemViewModelBuilder(commands.LayoutGridLarge)
                    {
                        IsToggle = true
                    }.Build(),
                    new ContextMenuFlyoutItemViewModelBuilder(commands.LayoutColumns)
                    {
                        IsToggle = true
                    }.Build(),
                    new ContextMenuFlyoutItemViewModelBuilder(commands.LayoutAdaptive)
                    {
                        IsToggle = true
                    }.Build(),
                },
            },
            new()
            {
                Text = "SortBy".GetLocalizedResource(),
                OpacityIcon = new OpacityIconModel()
                {
                    OpacityIconStyle = "ColorIconSort",
                },
                ShowItem = !itemsSelected,
                ShowInRecycleBin = true,
                ShowInSearchPage = true,
                ShowInFtpPage = true,
                ShowInZipPage = true,
                Items = new List<ContextMenuFlyoutItemViewModel>
                {
                    new ContextMenuFlyoutItemViewModelBuilder(commands.SortByName)
                    {
                        IsToggle = true
                    }.Build(),
                    new ContextMenuFlyoutItemViewModelBuilder(commands.SortByDateModified)
                    {
                        IsToggle = true
                    }.Build(),
                    new ContextMenuFlyoutItemViewModelBuilder(commands.SortByDateCreated)
                    {
                        IsToggle = true
                    }.Build(),
                    new ContextMenuFlyoutItemViewModelBuilder(commands.SortByType)
                    {
                        IsToggle = true
                    }.Build(),
                    new ContextMenuFlyoutItemViewModelBuilder(commands.SortBySize)
                    {
                        IsToggle = true
                    }.Build(),
                    new ContextMenuFlyoutItemViewModelBuilder(commands.SortBySyncStatus)
                    {
                        IsToggle = true
                    }.Build(),
                    new ContextMenuFlyoutItemViewModelBuilder(commands.SortByTag)
                    {
                        IsToggle = true
                    }.Build(),
                    new ContextMenuFlyoutItemViewModelBuilder(commands.SortByPath)
                    {
                        IsToggle = true
                    }.Build(),
                    new ContextMenuFlyoutItemViewModelBuilder(commands.SortByOriginalFolder)
                    {
                        IsToggle = true
                    }.Build(),
                    new ContextMenuFlyoutItemViewModelBuilder(commands.SortByDateDeleted)
                    {
                        IsToggle = true
                    }.Build(),
                    new ContextMenuFlyoutItemViewModel
                    {
                        ItemType = ContextMenuFlyoutItemType.Separator,
                        ShowInRecycleBin = true,
                        ShowInSearchPage = true,
                        ShowInFtpPage = true,
                        ShowInZipPage = true,
                    },
                    new ContextMenuFlyoutItemViewModelBuilder(commands.SortAscending)
                    {
                        IsToggle = true
                    }.Build(),
                    new ContextMenuFlyoutItemViewModelBuilder(commands.SortDescending)
                    {
                        IsToggle = true
                    }.Build(),
                },
            },
            new()
            {
                Text = "NavToolbarGroupByRadioButtons/Text".GetLocalizedResource(),
                Glyph = "\uF168",
                ShowItem = !itemsSelected,
                ShowInRecycleBin = true,
                ShowInSearchPage = true,
                ShowInFtpPage = true,
                ShowInZipPage = true,
                Items = new List<ContextMenuFlyoutItemViewModel>
                {
                    new ContextMenuFlyoutItemViewModelBuilder(commands.GroupByNone)
                    {
                        IsToggle = true
                    }.Build(),
                    new ContextMenuFlyoutItemViewModelBuilder(commands.GroupByName)
                    {
                        IsToggle = true
                    }.Build(),
                    new ContextMenuFlyoutItemViewModel()
                    {
                        Text = "DateModifiedLowerCase".GetLocalizedResource(),
                        ShowInRecycleBin = true,
                        ShowInSearchPage = true,
                        ShowInFtpPage = true,
                        ShowInZipPage = true,
                        Items = new List<ContextMenuFlyoutItemViewModel>
                        {
                            new ContextMenuFlyoutItemViewModelBuilder(commands.GroupByDateModifiedYear)
                            {
                                IsToggle = true
                            }.Build(),
                            new ContextMenuFlyoutItemViewModelBuilder(commands.GroupByDateModifiedMonth)
                            {
                                IsToggle = true
                            }.Build(),
                        },
                    },
                    new ContextMenuFlyoutItemViewModel()
                    {
                        Text = "DateCreated".GetLocalizedResource(),
                        ShowInRecycleBin = true,
                        ShowInSearchPage = true,
                        ShowInFtpPage = true,
                        ShowInZipPage = true,
                        Items = new List<ContextMenuFlyoutItemViewModel>
                        {
                            new ContextMenuFlyoutItemViewModelBuilder(commands.GroupByDateCreatedYear)
                            {
                                IsToggle = true
                            }.Build(),
                            new ContextMenuFlyoutItemViewModelBuilder(commands.GroupByDateCreatedMonth)
                            {
                                IsToggle = true
                            }.Build(),
                        },
                    },
                    new ContextMenuFlyoutItemViewModelBuilder(commands.GroupByType)
                    {
                        IsToggle = true
                    }.Build(),
                    new ContextMenuFlyoutItemViewModelBuilder(commands.GroupBySize)
                    {
                        IsToggle = true
                    }.Build(),
                    new ContextMenuFlyoutItemViewModelBuilder(commands.GroupBySyncStatus)
                    {
                        IsToggle = true
                    }.Build(),
                    new ContextMenuFlyoutItemViewModelBuilder(commands.GroupByTag)
                    {
                        IsToggle = true
                    }.Build(),
                    new ContextMenuFlyoutItemViewModelBuilder(commands.GroupByOriginalFolder)
                    {
                        IsToggle = true
                    }.Build(),
                    new ContextMenuFlyoutItemViewModel()
                    {
                        Text = "DateDeleted".GetLocalizedResource(),
                        ShowInRecycleBin = true,
                        IsHidden = !currentInstanceViewModel.IsPageTypeRecycleBin,
                        Items = new List<ContextMenuFlyoutItemViewModel>
                        {
                            new ContextMenuFlyoutItemViewModelBuilder(commands.GroupByDateDeletedYear)
                            {
                                IsToggle = true
                            }.Build(),
                            new ContextMenuFlyoutItemViewModelBuilder(commands.GroupByDateDeletedMonth)
                            {
                                IsToggle = true
                            }.Build(),
                        },
                    },
                    new ContextMenuFlyoutItemViewModelBuilder(commands.GroupByFolderPath)
                    {
                        IsToggle = true
                    }.Build(),
                    new ContextMenuFlyoutItemViewModel
                    {
                        ItemType = ContextMenuFlyoutItemType.Separator,
                        ShowInRecycleBin = true,
                        ShowInSearchPage = true,
                        ShowInFtpPage = true,
                        ShowInZipPage = true,
                    },
                    new ContextMenuFlyoutItemViewModelBuilder(commands.GroupAscending)
                    {
                        IsToggle = true,
                        IsVisible = true
                    }.Build(),
                    new ContextMenuFlyoutItemViewModelBuilder(commands.GroupDescending)
                    {
                        IsToggle = true,
                        IsVisible = true
                    }.Build(),
                },
            },*/
            new ContextMenuFlyoutItemViewModelBuilder(commands.RefreshItems)
            {
                IsVisible = !itemsSelected,
            }.Build(),
            new()
            {
                ItemType = ContextMenuFlyoutItemType.Separator,
                ShowInFtpPage = true,
                ShowInZipPage = true,
                ShowItem = !itemsSelected
            },
            new()
            {
                OpacityIcon = new OpacityIconModel()
                {
                    OpacityIconStyle = commands.AddItem.Glyph.OpacityStyle
                },
                Text = commands.AddItem.Label,
                Items = GetNewItemItems(commandsViewModel, currentInstanceViewModel.CanCreateFileInPage, commands),
                ShowItem = !itemsSelected,
                ShowInFtpPage = true
            },
            /*new ContextMenuFlyoutItemViewModelBuilder(commands.FormatDrive).Build(),
            new ContextMenuFlyoutItemViewModelBuilder(commands.EmptyRecycleBin)
            {
                IsVisible = currentInstanceViewModel.IsPageTypeRecycleBin && !itemsSelected,
            }.Build(),
            new ContextMenuFlyoutItemViewModelBuilder(commands.RestoreAllRecycleBin)
            {
                IsVisible = currentInstanceViewModel.IsPageTypeRecycleBin && !itemsSelected,
            }.Build(),
            new ContextMenuFlyoutItemViewModelBuilder(commands.RestoreRecycleBin)
            {
                IsVisible = currentInstanceViewModel.IsPageTypeRecycleBin && itemsSelected,
            }.Build(),*/
            new ContextMenuFlyoutItemViewModelBuilder(commands.OpenItem).Build(),
            new ContextMenuFlyoutItemViewModelBuilder(commands.OpenItemWithApplicationPicker)
            {
                Tag = "OpenWith",
            }.Build(),
            new()
            {
                Text = "OpenWith".GetLocalized(),
                OpacityIcon = new OpacityIconModel()
                {
                    OpacityIconStyle = "ColorIconOpenWith"
                },
                Tag = "OpenWithOverflow",
                IsHidden = true,
                CollapseLabel = true,
                Items = new List<ContextMenuFlyoutItemViewModel>() {
                    new()
                    {
                        Text = "Placeholder",
                        ShowInSearchPage = true,
                    }
                },
                ShowInSearchPage = true,
                ShowItem = itemsSelected && showOpenItemWith
            },
            /*new ContextMenuFlyoutItemViewModelBuilder(commands.OpenFileLocation).Build(),
            new ContextMenuFlyoutItemViewModelBuilder(commands.OpenDirectoryInNewTabAction).Build(),
            new ContextMenuFlyoutItemViewModelBuilder(commands.OpenInNewWindowItemAction).Build(),
            new ContextMenuFlyoutItemViewModelBuilder(commands.OpenDirectoryInNewPaneAction).Build(),*/
            /*new()
            {
                Text = "BaseLayoutItemContextFlyoutSetAs/Text".GetLocalizedResource(),
                ShowItem = itemsSelected && (selectedItemsPropertiesViewModel?.IsSelectedItemImage ?? false),
                ShowInSearchPage = true,
                Items = new List<ContextMenuFlyoutItemViewModel>
                {
                    new ContextMenuFlyoutItemViewModelBuilder(commands.SetAsWallpaperBackground).Build(),
                    new ContextMenuFlyoutItemViewModelBuilder(commands.SetAsLockscreenBackground).Build(),
                    new ContextMenuFlyoutItemViewModelBuilder(commands.SetAsSlideshowBackground).Build(),
                }
            },*/
            /*new ContextMenuFlyoutItemViewModelBuilder(commands.RotateLeft)
            {
                IsVisible = !currentInstanceViewModel.IsPageTypeRecycleBin
                            && !currentInstanceViewModel.IsPageTypeZipFolder
                            && (selectedItemsPropertiesViewModel?.IsSelectedItemImage ?? false)
            }.Build(),
            new ContextMenuFlyoutItemViewModelBuilder(commands.RotateRight)
            {
                IsVisible = !currentInstanceViewModel.IsPageTypeRecycleBin
                            && !currentInstanceViewModel.IsPageTypeZipFolder
                            && (selectedItemsPropertiesViewModel?.IsSelectedItemImage ?? false)
            }.Build(),
            new ContextMenuFlyoutItemViewModelBuilder(commands.RunAsAdmin).Build(),
            new ContextMenuFlyoutItemViewModelBuilder(commands.RunAsAnotherUser).Build(),*/
            new()
            {
                ItemType = ContextMenuFlyoutItemType.Separator,
                ShowInSearchPage = true,
                ShowInFtpPage = true,
                ShowInZipPage = true,
                ShowItem = itemsSelected
            },
            new ContextMenuFlyoutItemViewModelBuilder(commands.CutItem)
            {
                IsPrimary = true,
            }.Build(),
            new ContextMenuFlyoutItemViewModelBuilder(commands.CopyItem)
            {
                IsPrimary = true,
            }.Build(),
            new ContextMenuFlyoutItemViewModelBuilder(commands.PasteItemToSelection)
            {
                IsPrimary = true,
                IsVisible = true,
            }.Build(),
            new ContextMenuFlyoutItemViewModelBuilder(commands.CopyPath)
            {
                IsVisible = itemsSelected && !currentInstanceViewModel.IsPageTypeRecycleBin,
            }.Build(),
            new ContextMenuFlyoutItemViewModelBuilder(commands.CreateFolderWithSelection)
            {
                IsVisible = itemsSelected
            }.Build(),
            new ContextMenuFlyoutItemViewModelBuilder(commands.CreateShortcut)
            {
                IsVisible = itemsSelected && (!selectedItems.FirstOrDefault()?.IsShortcut ?? false)
                    && !currentInstanceViewModel.IsPageTypeRecycleBin,
            }.Build(),
            new ContextMenuFlyoutItemViewModelBuilder(commands.Rename)
            {
                IsPrimary = true,
                IsVisible = itemsSelected
            }.Build(),
            new ContextMenuFlyoutItemViewModelBuilder(commands.ShareItem)
            {
                IsPrimary = true
            }.Build(),
            new ContextMenuFlyoutItemViewModelBuilder(commands.DeleteItem)
            {
                IsVisible = itemsSelected,
                IsPrimary = true,
            }.Build(),
            /*new ContextMenuFlyoutItemViewModelBuilder(commands.OpenProperties)
            {
                IsPrimary = true,
                IsVisible = commands.OpenProperties.IsExecutable
            }.Build(),
            new ContextMenuFlyoutItemViewModelBuilder(commands.OpenParentFolder).Build(),*/
            /*new ContextMenuFlyoutItemViewModelBuilder(commands.PinItemToFavorites)
            {
                IsVisible = commands.PinItemToFavorites.IsExecutable && userSettingsService.GeneralSettingsService.ShowFavoritesSection,
            }.Build(),*/
            /*new ContextMenuFlyoutItemViewModelBuilder(commands.UnpinItemFromFavorites)
            {
                IsVisible = commands.UnpinItemFromFavorites.IsExecutable && userSettingsService.GeneralSettingsService.ShowFavoritesSection,
            }.Build(),*/
            /*new ContextMenuFlyoutItemViewModelBuilder(commands.PinToStart)
            {
                IsVisible = selectedItems.All(x => !x.IsShortcut && (x.PrimaryItemAttribute == StorageItemTypes.Folder || x.IsExecutable) && !x.IsArchive && !x.IsItemPinnedToStart),
                ShowOnShift = true,
            }.Build(),*/
            /*new ContextMenuFlyoutItemViewModelBuilder(commands.UnpinFromStart)
            {
                IsVisible = selectedItems.All(x => !x.IsShortcut && (x.PrimaryItemAttribute == StorageItemTypes.Folder || x.IsExecutable) && !x.IsArchive && x.IsItemPinnedToStart),
                ShowOnShift = true,
            }.Build(),*/
            /*new ContextMenuFlyoutItemViewModel
            {
                Text = "Compress".GetLocalizedResource(),
                ShowInSearchPage = true,
                OpacityIcon = new OpacityIconModel()
                {
                    OpacityIconStyle = "ColorIconZip",
                },
                Items = new List<ContextMenuFlyoutItemViewModel>
                {
                    new ContextMenuFlyoutItemViewModelBuilder(commands.CompressIntoArchive).Build(),
                    new ContextMenuFlyoutItemViewModelBuilder(commands.CompressIntoZip).Build(),
                    new ContextMenuFlyoutItemViewModelBuilder(commands.CompressIntoSevenZip).Build(),
                },
                ShowItem = itemsSelected && CompressHelper.CanCompress(selectedItems)
            },*/
            /*new ContextMenuFlyoutItemViewModel
            {
                Text = "Extract".GetLocalizedResource(),
                ShowInSearchPage = true,
                OpacityIcon = new OpacityIconModel()
                {
                    OpacityIconStyle = "ColorIconZip",
                },
                Items = new List<ContextMenuFlyoutItemViewModel>
                {
                    new ContextMenuFlyoutItemViewModelBuilder(commands.DecompressArchive).Build(),
                    new ContextMenuFlyoutItemViewModelBuilder(commands.DecompressArchiveHereSmart).Build(),
                    new ContextMenuFlyoutItemViewModelBuilder(commands.DecompressArchiveHere).Build(),
                    new ContextMenuFlyoutItemViewModelBuilder(commands.DecompressArchiveToChildFolder).Build(),
                },
                ShowItem = CompressHelper.CanDecompress(selectedItems)
            },*/
            /*new()
            {
                Text = "SendTo".GetLocalizedResource(),
                Tag = "SendTo",
                CollapseLabel = true,
                ShowInSearchPage = true,
                ShowItem = itemsSelected && userSettingsService.GeneralSettingsService.ShowSendToMenu
            },*/
            /*new()
            {
                Text = "SendTo".GetLocalizedResource(),
                Tag = "SendToOverflow",
                IsHidden = true,
                CollapseLabel = true,
                Items = new List<ContextMenuFlyoutItemViewModel>() {
                    new()
                    {
                        Text = "Placeholder",
                        ShowInSearchPage = true,
                    }
                },
                ShowInSearchPage = true,
                ShowItem = itemsSelected && userSettingsService.GeneralSettingsService.ShowSendToMenu
            },*/
            /*new()
            {
                Text = "TurnOnBitLocker".GetLocalizedResource(),
                Tag = "TurnOnBitLockerPlaceholder",
                CollapseLabel = true,
                IsEnabled = false,
                ShowItem = isDriveRoot
            },*/
            /*new()
            {
                Text = "ManageBitLocker".GetLocalizedResource(),
                Tag = "ManageBitLockerPlaceholder",
                CollapseLabel = true,
                ShowItem = isDriveRoot,
                IsEnabled = false
            },*/
			// Shell extensions are not available on the FTP server or in the archive,
			// but following items are intentionally added because icons in the context menu will not appear
			// unless there is at least one menu item with an icon that is not an OpacityIcon. (#12943)
			new()
            {
                ItemType = ContextMenuFlyoutItemType.Separator,
                Tag = "OverflowSeparator",
                ShowInFtpPage = true,
                ShowInZipPage = true,
                ShowInRecycleBin = true,
                ShowInSearchPage = true,
            },
            new()
            {
                Text = "Loading".GetLocalized(),
                Glyph = "\xE712",
                Items = new List<ContextMenuFlyoutItemViewModel>(),
                ID = "ItemOverflow",
                Tag = "ItemOverflow",
                ShowInFtpPage = true,
                ShowInZipPage = true,
                ShowInRecycleBin = true,
                ShowInSearchPage = true,
                IsEnabled = false
            },
        }.Where(x => x.ShowItem).ToList();

        return list;
    }

    public static List<ContextMenuFlyoutItemViewModel> Filter(
        List<ContextMenuFlyoutItemViewModel> items, 
        List<ListedItem> selectedItems, 
        bool shiftPressed, 
        CurrentInstanceViewModel currentInstanceViewModel, 
        bool removeOverflowMenu = true)
    {
        items = items.Where(x => Check(item: x, currentInstanceViewModel: currentInstanceViewModel, selectedItems: selectedItems)).ToList();
        items.ForEach(x => x.Items = x.Items?.Where(y => Check(item: y, currentInstanceViewModel: currentInstanceViewModel, selectedItems: selectedItems)).ToList()!);

        var overflow = items.Where(x => x.ID == "ItemOverflow").FirstOrDefault();
        if (overflow is not null)
        {
            if (!shiftPressed )//&& userSettingsService.GeneralSettingsService.MoveShellExtensionsToSubMenu) // items with ShowOnShift to overflow menu
            {
                var overflowItems = items.Where(x => x.ShowOnShift).ToList();

                // Adds a separator between items already there and the new ones
                if (overflow.Items.Count != 0 && overflowItems.Count > 0 && overflow.Items.Last().ItemType != ContextMenuFlyoutItemType.Separator)
                {
                    overflow.Items.Add(new ContextMenuFlyoutItemViewModel { ItemType = ContextMenuFlyoutItemType.Separator });
                }

                items = items.Except(overflowItems).ToList();
                overflow.Items.AddRange(overflowItems);
            }

            // remove the overflow if it has no child items
            if (overflow.Items.Count == 0 && removeOverflowMenu)
            {
                items.Remove(overflow);
            }
        }

        return items;
    }

    private static bool Check(
        ContextMenuFlyoutItemViewModel item, 
        CurrentInstanceViewModel currentInstanceViewModel, 
        List<ListedItem> selectedItems)
    {
        return (item.ShowInRecycleBin || !currentInstanceViewModel.IsPageTypeRecycleBin)
            && (item.ShowInSearchPage || !currentInstanceViewModel.IsPageTypeSearchResults)
            && (item.ShowInFtpPage || !currentInstanceViewModel.IsPageTypeFtp)
            && (item.ShowInZipPage || !currentInstanceViewModel.IsPageTypeZipFolder)
            && (!item.SingleItemOnly || selectedItems.Count == 1)
            && item.ShowItem;
    }

    public static List<ContextMenuFlyoutItemViewModel> GetNewItemItems(
        BaseLayoutViewModel commandsViewModel, 
        bool canCreateFileInPage,
        ICommandManager commands)
    {
        var list = new List<ContextMenuFlyoutItemViewModel>()
        {
            new ContextMenuFlyoutItemViewModelBuilder(commands.CreateFolder).Build(),
            new()
            {
                Text = "File".GetLocalized(),
                Glyph = "\uE7C3",
                Command = commandsViewModel.CreateNewFileCommand,
                ShowInFtpPage = true,
                ShowInZipPage = true,
                IsEnabled = canCreateFileInPage
            },
            new ContextMenuFlyoutItemViewModelBuilder(commands.CreateShortcutFromDialog).Build(),
            new()
            {
                ItemType = ContextMenuFlyoutItemType.Separator,
            }
        };

        if (canCreateFileInPage)
        {
            var cachedNewContextMenuEntries = addItemService.GetEntries();
            cachedNewContextMenuEntries?.ForEach(i =>
            {
                if (!string.IsNullOrEmpty(i.IconBase64))
                {
                    // loading the bitmaps takes a while, so this caches them
                    var bitmapData = Convert.FromBase64String(i.IconBase64);
                    using var ms = new MemoryStream(bitmapData);
                    var bitmap = new BitmapImage();
                    _ = bitmap.SetSourceAsync(ms.AsRandomAccessStream());
                    list.Add(new ContextMenuFlyoutItemViewModel()
                    {
                        Text = i.Name,
                        BitmapIcon = bitmap,
                        Command = commandsViewModel.CreateNewFileCommand,
                        CommandParameter = i,
                    });
                }
                else
                {
                    list.Add(new ContextMenuFlyoutItemViewModel()
                    {
                        Text = i.Name,
                        Glyph = "\xE7C3",
                        Command = commandsViewModel.CreateNewFileCommand,
                        CommandParameter = i,
                    });
                }
            });
        }

        return list;
    }

    #endregion

    #region shell menu

    public static Task<List<ContextMenuFlyoutItemViewModel>> GetItemContextShellCommandsAsync(string workingDir, List<ListedItem> selectedItems, bool shiftPressed, bool showOpenMenu, CancellationToken cancellationToken)
            => ShellContextmenuHelper.GetShellContextmenuAsync(shiftPressed: shiftPressed, showOpenMenu: showOpenMenu, workingDirectory: workingDir, selectedItems: selectedItems, cancellationToken: cancellationToken);

    public static void SwapPlaceholderWithShellOption(CommandBarFlyout contextMenu, string placeholderName, ContextMenuFlyoutItemViewModel? replacingItem, int position)
    {
        var placeholder = contextMenu.SecondaryCommands
                                                        .Where(x => Equals((x as AppBarButton)?.Tag, placeholderName))
                                                        .FirstOrDefault() as AppBarButton;
        if (placeholder is not null)
        {
            placeholder.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        }

        if (replacingItem is not null)
        {
            var (_, bitLockerCommands) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(new List<ContextMenuFlyoutItemViewModel>() { replacingItem });
            contextMenu.SecondaryCommands.Insert(
                position,
                bitLockerCommands.FirstOrDefault()
            );
        }
    }

    #endregion
}