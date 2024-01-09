// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Concurrent;
using System.Net;
using System.Text;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.ViewModels.Pages.Widget;
using DesktopWidgets3.Files.App.Utils;
using DesktopWidgets3.Files.App.Utils.Storage;
using DesktopWidgets3.Files.App.Utils.Storage.Helpers;
using DesktopWidgets3.Files.Core.Data.Enums;
using DesktopWidgets3.Files.Shared.Extensions;
using Microsoft.UI.Dispatching;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using DesktopWidgets3.Files.App.Extensions;
using DesktopWidgets3.Files.Core.ViewModels.Dialogs;
using DesktopWidgets3.Files.Core.Data.Items;

namespace DesktopWidgets3.Files.App.Helpers;

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
                        var result = await viewModel.FileSystemViewModel.GetFileFromPathAsync(listedItem.ItemPath)
                            .OnSuccess(t => items.Add(t));

                        if (!result)
                        {
                            throw new IOException($"Failed to process {listedItem.ItemPath}.", (int)result.ErrorCode);
                        }
                    }
                    else
                    {
                        var result = await viewModel.FileSystemViewModel.GetFolderFromPathAsync(listedItem.ItemPath)
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

    #region Cut

    public static async Task CutItemAsync(FolderViewViewModel viewModel)
    {
        var dataPackage = new DataPackage()
        {
            RequestedOperation = DataPackageOperation.Move
        };
        ConcurrentBag<IStorageItem> items = new();

        if (viewModel.IsItemSelected)
        {
            // First, reset DataGrid Rows that may be in "cut" command mode
            viewModel.ItemManipulationModel.RefreshItemsOpacity();

            var itemsCount = viewModel.SelectedItems!.Count;

            /*var banner = itemsCount > 50 ? StatusCenterHelper.AddCard_Prepare() : null;*/

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

                    // FTP don't support cut, fallback to copy
                    if (listedItem is not FtpItem)
                    {
                        _ = DesktopWidgets3.App.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                        {
                            // Dim opacities accordingly
                            listedItem.Opacity = Constants.UI.DimItemOpacity;
                        });
                    }
                    if (listedItem is FtpItem ftpItem)
                    {
                        if (ftpItem.PrimaryItemAttribute is StorageItemTypes.File or StorageItemTypes.Folder)
                        {
                            items.Add(await ftpItem.ToStorageItem());
                        }
                    }
                    else if (listedItem.PrimaryItemAttribute == StorageItemTypes.File || listedItem is ZipItem)
                    {
                        var result = await viewModel.FileSystemViewModel.GetFileFromPathAsync(listedItem.ItemPath)
                            .OnSuccess(t => items.Add(t));

                        if (!result)
                        {
                            throw new IOException($"Failed to process {listedItem.ItemPath}.", (int)result.ErrorCode);
                        }
                    }
                    else
                    {
                        var result = await viewModel.FileSystemViewModel.GetFolderFromPathAsync(listedItem.ItemPath)
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
                    var filePaths = viewModel.SelectedItems.Select(x => x.ItemPath).ToArray();

                    await FileOperationsHelpers.SetClipboard(filePaths, DataPackageOperation.Move);

                    /*_statusCenterViewModel.RemoveItem(banner);*/

                    return;
                }

                viewModel.ItemManipulationModel.RefreshItemsOpacity();

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

    #region Paste

    public static async Task PasteItemAsync(string destinationPath, FolderViewViewModel viewModel)
    {
        var packageView = await FilesystemTasks.Wrap(() => Task.FromResult(Clipboard.GetContent()));
        if (packageView && packageView.Result is not null)
        {
            await viewModel.FileSystemHelpers.PerformOperationTypeAsync(viewModel, packageView.Result.RequestedOperation, packageView, destinationPath, false);
            viewModel.ItemManipulationModel.RefreshItemsOpacity();
            await viewModel.RefreshIfNoWatcherExistsAsync();
        }
    }

    #endregion

    #region create shortcuts

    public static async Task CreateShortcutAsync(FolderViewViewModel viewModel, IReadOnlyList<ListedItem> selectedItems)
    {
        var currentPath = viewModel.FileSystemViewModel.WorkingDirectory;

        /*if (App.LibraryManager.TryGetLibrary(currentPath ?? string.Empty, out var library) && !library.IsEmpty)
        {
            currentPath = library.DefaultSaveFolder;
        }*/

        foreach (var selectedItem in selectedItems)
        {
            var fileName = string.Format("ShortcutCreateNewSuffix".GetLocalized(), selectedItem.Name) + ".lnk";
            var filePath = Path.Combine(currentPath ?? string.Empty, fileName);

            if (!await FileOperationsHelpers.CreateOrUpdateLinkAsync(filePath, selectedItem.ItemPath))
            {
                await HandleShortcutCannotBeCreated(viewModel, fileName, selectedItem.ItemPath);
            }
        }

        if (viewModel is not null)
        {
            await viewModel.RefreshIfNoWatcherExistsAsync();
        }
    }

    public static async Task<bool> HandleShortcutCannotBeCreated(FolderViewViewModel viewModel, string shortcutName, string destinationPath)
    {
        var result = await DialogDisplayHelper.ShowDialogAsync
        (
            viewModel,
            "CannotCreateShortcutDialogTitle".GetLocalized(),
            "CannotCreateShortcutDialogMessage".GetLocalized(),
            "Create".GetLocalized(),
            "Cancel".GetLocalized()
        );
        if (!result)
        {
            return false;
        }

        var shortcutPath = Path.Combine(Constants.UserEnvironmentPaths.DesktopPath, shortcutName);

        return await FileOperationsHelpers.CreateOrUpdateLinkAsync(shortcutPath, destinationPath);
    }

    #endregion

    #region create folder

    public static async Task CreateFolderWithSelectionAsync(FolderViewViewModel viewModel)
    {
        try
        {
            var items = viewModel.SelectedItems.ToList().Select((item) => StorageHelpers.FromPathAndType(
                item.ItemPath,
                item.PrimaryItemAttribute == StorageItemTypes.File ? FilesystemItemType.File : FilesystemItemType.Directory));
            var folder = await CreateFileFromDialogResultTypeForResult(AddItemDialogItemType.Folder, null, viewModel);
            if (folder is null)
            {
                return;
            }

            await viewModel.FileSystemHelpers.MoveItemsAsync(viewModel, items, items.Select(x => PathNormalization.Combine(folder.Path, x.Name)), false);
            await viewModel.RefreshIfNoWatcherExistsAsync();
        }
        catch (Exception)
        {
        }
    }

    private static async Task<IStorageItem?> CreateFileFromDialogResultTypeForResult(AddItemDialogItemType itemType, ShellNewEntry? itemInfo, FolderViewViewModel viewModel)
    {
        string? currentPath = null;

        if (viewModel is not null)
        {
            currentPath = viewModel.FileSystemViewModel.WorkingDirectory;
            /*if (App.LibraryManager.TryGetLibrary(currentPath, out var library) &&
                !library.IsEmpty &&
                library.Folders.Count == 1) // TODO: handle libraries with multiple folders
            {
                currentPath = library.Folders.First();
            }*/
        }

        // Skip rename dialog when ShellNewEntry has a Command (e.g. ".accdb", ".gdoc")
        string? userInput = null;
        if (itemType != AddItemDialogItemType.File || itemInfo?.Command is null)
        {
            var dialog = DynamicDialogFactory.GetFor_RenameDialog();
            await dialog.TryShowAsync(viewModel!); // Show rename dialog

            if (dialog.DynamicResult != DynamicDialogResult.Primary)
            {
                return null;
            }

            userInput = dialog.ViewModel.AdditionalData as string;
        }

        // Create file based on dialog result
        (ReturnResult Status, IStorageItem? Item) created = (ReturnResult.Failed, null);
        switch (itemType)
        {
            case AddItemDialogItemType.Folder:
                userInput = !string.IsNullOrWhiteSpace(userInput) ? userInput : "NewFolder".GetLocalized();
                created = await viewModel!.FileSystemHelpers.CreateAsync(
                    viewModel,
                    StorageHelpers.FromPathAndType(PathNormalization.Combine(currentPath!, userInput), FilesystemItemType.Directory));
                break;

            case AddItemDialogItemType.File:
                userInput = !string.IsNullOrWhiteSpace(userInput) ? userInput : itemInfo?.Name ?? "NewFile".GetLocalized();
                created = await viewModel!.FileSystemHelpers.CreateAsync(
                    viewModel,
                    StorageHelpers.FromPathAndType(PathNormalization.Combine(currentPath!, userInput + itemInfo?.Extension), FilesystemItemType.File));
                break;
        }

        if (created.Status == ReturnResult.AccessUnauthorized)
        {
            await DialogDisplayHelper.ShowDialogAsync
            (
                viewModel!,
                "AccessDenied".GetLocalized(),
                "AccessDeniedCreateDialog/Text".GetLocalized()
            );
        }

        return created.Item;
    }

    #endregion

    #region require password

    public static async Task<StorageCredential> RequestPassword(FolderViewViewModel viewModel, IPasswordProtectedItem sender)
    {
        var path = ((IStorageItem)sender).Path;
        var isFtp = FtpHelpers.IsFtpPath(path);

        var credentialDialogViewModel = new CredentialDialogViewModel() { CanBeAnonymous = isFtp, PasswordOnly = !isFtp };
        var dialogService = viewModel.DialogService;
        var dialogResult = await DesktopWidgets3.App.DispatcherQueue.EnqueueOrInvokeAsync(() =>
            dialogService.ShowDialogAsync(credentialDialogViewModel));

        if (dialogResult != DialogResult.Primary || credentialDialogViewModel.IsAnonymous)
        {
            return new();
        }

        // Can't do more than that to mitigate immutability of strings. Perhaps convert DisposableArray to SecureString immediately?
        var credentials = new StorageCredential(credentialDialogViewModel.UserName, Encoding.UTF8.GetString(credentialDialogViewModel.Password!));
        credentialDialogViewModel.Password?.Dispose();

        if (isFtp)
        {
            var host = FtpHelpers.GetFtpHost(path);
            Storage.FtpStorage.FtpManager.Credentials[host] = new NetworkCredential(credentials.UserName, credentials.SecurePassword);
        }

        return credentials;
    }

    #endregion
}
