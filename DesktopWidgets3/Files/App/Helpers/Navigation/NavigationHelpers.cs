// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.ViewModels.Pages.Widget;
using Files.App.Utils.Storage;
using Files.Core.Data.Enums;
using Files.Core.Data.Items;
using Files.Core.Helpers;
using Files.Shared.Helpers;
using Windows.Storage;

namespace Files.App.Helpers;

public static class NavigationHelpers
{
    public static async Task OpenSelectedItemsAsync(FolderViewViewModel viewModel, bool openViaApplicationPicker = false)
    {
        // Don't open files and folders inside recycle bin
        if (viewModel.WorkingDirectory.StartsWith(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.Ordinal) ||
            viewModel.SelectedItems is null)
        {
            return;
        }

        var selectedItems = viewModel.SelectedItems.ToList();
        var opened = false;

        if (selectedItems.Count == 1)
        {
            // If only one folder is selected, try to navigate to it
            if (selectedItems[0].PrimaryItemAttribute == StorageItemTypes.Folder)
            {
                opened = await viewModel.NavigateToPath(selectedItems[0].ItemPath);
            }
        }

        // If multiple files are selected, open them together
        if (!openViaApplicationPicker && selectedItems.Count > 1 &&
            selectedItems.All(x => x.PrimaryItemAttribute == StorageItemTypes.File && !x.IsExecutable && !x.IsShortcut))
        {
            opened = await Win32Helpers.InvokeWin32ComponentAsync(string.Join('|', selectedItems.Select(x => x.ItemPath)), viewModel);
        }

        if (opened)
        {
            return;
        }

        foreach (var item in selectedItems)
        {
            var type = item.PrimaryItemAttribute == StorageItemTypes.Folder
                ? FilesystemItemType.Directory
                : FilesystemItemType.File;

            /*await OpenPath(item.ItemPath, viewModel, type, false, openViaApplicationPicker, forceOpenInNewTab: forceOpenInNewTab);

            if (type == FilesystemItemType.Directory)
                forceOpenInNewTab = true;*/
        }
    }

    /*public static async Task<bool> OpenPath(string path, FolderViewViewModel viewModel, FilesystemItemType? itemType = null, bool openSilent = false, bool openViaApplicationPicker = false, IEnumerable<string>? selectItems = null, string? args = default, bool forceOpenInNewTab = false)
    {
        string previousDir = associatedInstance.FilesystemViewModel.WorkingDirectory;
        bool isHiddenItem = NativeFileOperationsHelper.HasFileAttribute(path, System.IO.FileAttributes.Hidden);
        bool isDirectory = NativeFileOperationsHelper.HasFileAttribute(path, System.IO.FileAttributes.Directory);
        bool isReparsePoint = NativeFileOperationsHelper.HasFileAttribute(path, System.IO.FileAttributes.ReparsePoint);
        bool isShortcut = FileExtensionHelpers.IsShortcutOrUrlFile(path);
        bool isScreenSaver = FileExtensionHelpers.IsScreenSaverFile(path);
        bool isTag = path.StartsWith("tag:");
        FilesystemResult opened = (FilesystemResult)false;

        if (isTag)
        {
            if (!forceOpenInNewTab)
            {
                associatedInstance.NavigateToPath(path, new NavigationArguments()
                {
                    IsSearchResultPage = true,
                    SearchPathParam = "Home",
                    SearchQuery = path,
                    AssociatedTabInstance = associatedInstance,
                    NavPathParam = path
                });
            }
            else
            {
                await NavigationHelpers.OpenPathInNewTab(path);
            }

            return true;
        }

        var shortcutInfo = new ShellLinkItem();
        if (itemType is null || isShortcut || isHiddenItem || isReparsePoint)
        {
            if (isShortcut)
            {
                var shInfo = await FileOperationsHelpers.ParseLinkAsync(path);

                if (shInfo is null)
                    return false;

                itemType = shInfo.IsFolder ? FilesystemItemType.Directory : FilesystemItemType.File;

                shortcutInfo = shInfo;

                if (shortcutInfo.InvalidTarget)
                {
                    if (await DialogDisplayHelper.ShowDialogAsync(DynamicDialogFactory.GetFor_ShortcutNotFound(shortcutInfo.TargetPath)) != DynamicDialogResult.Primary)
                        return false;

                    // Delete shortcut
                    var shortcutItem = StorageHelpers.FromPathAndType(path, FilesystemItemType.File);
                    await associatedInstance.FilesystemHelpers.DeleteItemAsync(shortcutItem, DeleteConfirmationPolicies.Never, false, true);
                }
            }
            else if (isReparsePoint)
            {
                if (!isDirectory &&
                    NativeFindStorageItemHelper.GetWin32FindDataForPath(path, out var findData) &&
                    findData.dwReserved0 == NativeFileOperationsHelper.IO_REPARSE_TAG_SYMLINK)
                {
                    shortcutInfo.TargetPath = NativeFileOperationsHelper.ParseSymLink(path);
                }
                itemType ??= isDirectory ? FilesystemItemType.Directory : FilesystemItemType.File;
            }
            else if (isHiddenItem)
            {
                itemType = NativeFileOperationsHelper.HasFileAttribute(path, System.IO.FileAttributes.Directory) ? FilesystemItemType.Directory : FilesystemItemType.File;
            }
            else
            {
                itemType = await StorageHelpers.GetTypeFromPath(path);
            }
        }

        switch (itemType)
        {
            case FilesystemItemType.Library:
                opened = await OpenLibrary(path, associatedInstance, selectItems, forceOpenInNewTab);
                break;

            case FilesystemItemType.Directory:
                opened = await OpenDirectory(path, associatedInstance, selectItems, shortcutInfo, forceOpenInNewTab);
                break;

            case FilesystemItemType.File:
                // Starts the screensaver in full-screen mode
                if (isScreenSaver)
                    args += "/s";

                opened = await OpenFile(path, associatedInstance, shortcutInfo, openViaApplicationPicker, args);
                break;
        };

        if (opened.ErrorCode == FileSystemStatusCode.NotFound && !openSilent)
        {
            await DialogDisplayHelper.ShowDialogAsync("FileNotFoundDialog/Title".GetLocalizedResource(), "FileNotFoundDialog/Text".GetLocalizedResource());
            associatedInstance.ToolbarViewModel.CanRefresh = false;
            associatedInstance.FilesystemViewModel?.RefreshItems(previousDir);
        }

        return opened;
    }*/
}