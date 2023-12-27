﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Concurrent;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.ViewModels.Pages.Widget;
using Files.App.Utils;
using Files.App.Utils.Storage;
using Files.App.Utils.Storage.Helpers;
using Files.Core.Data.Enums;
using Files.Shared.Extensions;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace Files.App.Helpers;

public static class UIFileSystemHelpers
{
    /*public static async Task CreateFileFromDialogResultTypeAsync(AddItemDialogItemType itemType, ShellNewEntry? itemInfo)//IShellPage associatedInstance)
    {
        await CreateFileFromDialogResultTypeForResult(itemType, itemInfo);//associatedInstance);
        await associatedInstance.RefreshIfNoWatcherExistsAsync();
    }*/

    /*private static async Task<IStorageItem?> CreateFileFromDialogResultTypeForResult(AddItemDialogItemType itemType, ShellNewEntry? itemInfo)//IShellPage associatedInstance)
    {
        string? currentPath = null;

        if (associatedInstance.SlimContentPage is not null)
        {
            currentPath = associatedInstance.FilesystemViewModel.WorkingDirectory;
            if (App.LibraryManager.TryGetLibrary(currentPath, out var library) &&
                !library.IsEmpty &&
                library.Folders.Count == 1) // FILESTODO: handle libraries with multiple folders
            {
                currentPath = library.Folders.First();
            }
        }

        // Skip rename dialog when ShellNewEntry has a Command (e.g. ".accdb", ".gdoc")
        string? userInput = null;
        if (itemType != AddItemDialogItemType.File || itemInfo?.Command is null)
        {
            DynamicDialog dialog = DynamicDialogFactory.GetFor_RenameDialog();
            await dialog.TryShowAsync(); // Show rename dialog

            if (dialog.DynamicResult != DynamicDialogResult.Primary)
            {
                return null;
            }

            userInput = dialog.ViewModel.AdditionalData as string;
        }

        // Create file based on dialog result
        (ReturnResult Status, IStorageItem Item) created = (ReturnResult.Failed, null);
        switch (itemType)
        {
            case AddItemDialogItemType.Folder:
                userInput = !string.IsNullOrWhiteSpace(userInput) ? userInput : "NewFolder".GetLocalizedResource();
                created = await associatedInstance.FilesystemHelpers.CreateAsync(
                    StorageHelpers.FromPathAndType(PathNormalization.Combine(currentPath, userInput), FilesystemItemType.Directory),
                    true);
                break;

            case AddItemDialogItemType.File:
                userInput = !string.IsNullOrWhiteSpace(userInput) ? userInput : itemInfo?.Name ?? "NewFile".GetLocalizedResource();
                created = await associatedInstance.FilesystemHelpers.CreateAsync(
                    StorageHelpers.FromPathAndType(PathNormalization.Combine(currentPath, userInput + itemInfo?.Extension), FilesystemItemType.File),
                    true);
                break;
        }

        if (created.Status == ReturnResult.AccessUnauthorized)
        {
            await DialogDisplayHelper.ShowDialogAsync
            (
                "AccessDenied".GetLocalizedResource(),
                "AccessDeniedCreateDialog/Text".GetLocalizedResource()
            );
        }

        return created.Item;
    }*/

    #region Rename

    public static async Task<bool> RenameFileItemAsync(FolderViewViewModel viewModel, ListedItem item, string newName, bool showExtensionDialog = true)
    {
        if (item is AlternateStreamItem ads) // For alternate streams Name is not a substring ItemNameRaw
        {
            newName = item.ItemNameRaw.Replace(
                item.Name[(item.Name.LastIndexOf(':') + 1)..],
                newName[(newName.LastIndexOf(':') + 1)..],
                StringComparison.Ordinal);
            newName = $"{ads.MainStreamName}:{newName}";
        }
        else if (string.IsNullOrEmpty(item.Name))
        {
            newName = string.Concat(newName, item.FileExtension);
        }
        else
        {
            newName = item.ItemNameRaw.Replace(item.Name, newName, StringComparison.Ordinal);
        }

        if (item.ItemNameRaw == newName || string.IsNullOrEmpty(newName))
        {
            return true;
        }

        var itemType = (item.PrimaryItemAttribute == StorageItemTypes.Folder) ? FilesystemItemType.Directory : FilesystemItemType.File;

        var renamed = await viewModel.FileSystemHelpers.RenameAsync(viewModel, StorageHelpers.FromPathAndType(item.ItemPath, itemType), newName, NameCollisionOption.FailIfExists, showExtensionDialog);

        if (renamed == ReturnResult.Success)
        {
            /*associatedInstance.ToolbarViewModel.CanGoForward = false;*/
            await viewModel.RefreshIfNoWatcherExistsAsync();
            return true;
        }

        return false;
    }

    #endregion

    #region Copy

    public static async Task CopyItemAsync(FolderViewViewModel viewModel)
    {
        var dataPackage = new DataPackage()
        {
            RequestedOperation = DataPackageOperation.Copy
        };
        ConcurrentBag<IStorageItem> items = new();

        if (viewModel.IsItemSelected)
        {
            viewModel.ItemManipulationModel.RefreshItemsOpacity();

            var itemsCount = viewModel.SelectedItems!.Count;

            /*var banner = itemsCount > 50 ? StatusCenterHelper.AddCard_Prepare() : null;*/

            var filePaths = viewModel.SelectedItems.Select(x => x.ItemPath).ToArray();

            await FileOperationsHelpers.SetClipboard(filePaths, DataPackageOperation.Copy);

            try
            {
                /*if (banner is not null)
                {
                    banner.Progress.EnumerationCompleted = true;
                    banner.Progress.ItemsCount = items.Count;
                    banner.Progress.ReportStatus(FileSystemStatusCode.InProgress);
                }*/
                await viewModel.SelectedItems.ToList().ParallelForEachAsync(async listedItem =>
                {
                    /*if (banner is not null)
                    {
                        banner.Progress.AddProcessedItemsCount(1);
                        banner.Progress.Report();
                    }*/

                    if (listedItem is FtpItem ftpItem)
                    {
                        if (ftpItem.PrimaryItemAttribute is StorageItemTypes.File or StorageItemTypes.Folder)
                        {
                            items.Add(await ftpItem.ToStorageItem());
                        }
                    }
                    else if (listedItem.PrimaryItemAttribute == StorageItemTypes.File || listedItem is ZipItem)
                    {
                        var result = await viewModel.ItemViewModel.GetFileFromPathAsync(listedItem.ItemPath)
                            .OnSuccess(t => items.Add(t));

                        if (!result)
                        {
                            throw new IOException($"Failed to process {listedItem.ItemPath}.", (int)result.ErrorCode);
                        }
                    }
                    else
                    {
                        var result = await viewModel.ItemViewModel.GetFolderFromPathAsync(listedItem.ItemPath)
                            .OnSuccess(t => items.Add(t));

                        if (!result)
                        {
                            throw new IOException($"Failed to process {listedItem.ItemPath}.", (int)result.ErrorCode);
                        }
                    }
                }, 10, /*banner?.CancellationToken ?? */default);
            }
            catch (Exception ex)
            {
                if (ex.HResult == (int)FileSystemStatusCode.Unauthorized)
                {
                    filePaths = viewModel.SelectedItems.Select(x => x.ItemPath).ToArray();

                    await FileOperationsHelpers.SetClipboard(filePaths, DataPackageOperation.Copy);

                    /*_statusCenterViewModel.RemoveItem(banner);*/

                    return;
                }

                /*_statusCenterViewModel.RemoveItem(banner);*/

                return;
            }

            /*_statusCenterViewModel.RemoveItem(banner);*/
        }

        var onlyStandard = items.All(x => x is StorageFile || x is StorageFolder || x is SystemStorageFile || x is SystemStorageFolder);
        if (onlyStandard)
        {
            items = new ConcurrentBag<IStorageItem>(await items.ToStandardStorageItemsAsync());
        }

        if (!items.Any())
        {
            return;
        }

        dataPackage.Properties.PackageFamilyName = InfoHelper.GetFamilyName();
        dataPackage.SetStorageItems(items, false);

        try
        {
            Clipboard.SetContent(dataPackage);
        }
        catch
        {
            dataPackage = null;
        }
    }

    #endregion
}
