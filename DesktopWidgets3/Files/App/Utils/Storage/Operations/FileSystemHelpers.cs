// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.ViewModels.Pages.Widget;
using Files.App.Helpers;
using Files.App.Utils.RecycleBin;
using Files.App.Utils.Storage;
using Files.Core.Data.Enums;
using Files.Core.Data.Items;
using Files.Shared.Extensions;
using Files.Shared.Helpers;
using Windows.Storage;
using static DesktopWidgets3.Services.DialogService;

namespace DesktopWidgets3.Services;

public sealed class FileSystemHelpers : IFileSystemHelpers
{
    #region delete items

    public async Task<ReturnResult> DeleteItemsAsync(
        FolderViewViewModel viewModel, 
        IEnumerable<IStorageItemWithPath> source, 
        DeleteConfirmationPolicies showDialog, 
        bool permanently)
    {
        source = await source.ToListAsync();

        var returnStatus = ReturnResult.InProgress;

        var deleteFromRecycleBin = source.Select(item => item.Path).Any(RecycleBinHelpers.IsPathUnderRecycleBin);
        var canBeSentToBin = !deleteFromRecycleBin && await RecycleBinHelpers.HasRecycleBin(source.FirstOrDefault()?.Path);

        // Get the delete type from the dialog
        if (showDialog is DeleteConfirmationPolicies.Always ||
            showDialog is DeleteConfirmationPolicies.PermanentOnly &&
            (permanently || !canBeSentToBin))
        {
            var dialogService = App.GetService<IDialogService>();

            // Return if the result isn't delete
            if (await dialogService.ShowDeleteWidgetDialog(viewModel.WidgetWindow) != DialogResult.Left)
            {
                return ReturnResult.Cancelled;
            }

            // TODO: Add permanent delete option here!
            // Delete selected items if the result is Yes
            //permanently = dialogViewModel.DeletePermanently;
        }
        else
        {
            // Delete permanently if recycle bin is not supported
            permanently |= !canBeSentToBin;
        }

        // Delete file
        returnStatus = await DeleteItemsAsync(viewModel, (IList<IStorageItemWithPath>)source, permanently);

        return returnStatus;
    }

    public async Task<ReturnResult> DeleteItemsAsync(FolderViewViewModel viewModel, IList<IStorageItemWithPath> source, bool permanently, bool asAdmin = false)
    {
        var originalPermanently = permanently;
        var result = ReturnResult.Success;

        for (var i = 0; i < source.Count; i++)
        {
            permanently = RecycleBinHelpers.IsPathUnderRecycleBin(source[i].Path) || originalPermanently;
            if (await DeleteAsync(viewModel, source[i], permanently) != ReturnResult.Success)
            {
                result = ReturnResult.Failed;
            };
        }

        return result;
    }

    public async Task<ReturnResult> DeleteAsync(FolderViewViewModel viewModel, IStorageItemWithPath source, bool permanently)
    {
        var deleteFromRecycleBin = RecycleBinHelpers.IsPathUnderRecycleBin(source.Path);
        FilesystemResult fsResult = FileSystemStatusCode.InProgress;

        // use generic delete method
        if (permanently)
        {
            fsResult = (FilesystemResult)NativeFileOperationsHelper.DeleteFileFromApp(source.Path);
        }

        // use other delete method
        if (!fsResult)
        {
            if (source.ItemType == FilesystemItemType.File)
            {
                fsResult = await viewModel.GetFileFromPathAsync(source.Path)
                    .OnSuccess((t) => t.DeleteAsync(permanently ? StorageDeleteOption.PermanentDelete : StorageDeleteOption.Default).AsTask());
            }
            else if (source.ItemType == FilesystemItemType.Directory)
            {
                fsResult = await viewModel.GetFolderFromPathAsync(source.Path)
                    .OnSuccess((t) => t.DeleteAsync(permanently ? StorageDeleteOption.PermanentDelete : StorageDeleteOption.Default).AsTask());
            }
        }

        if (fsResult == FileSystemStatusCode.Unauthorized)
        {
            // Cannot do anything, already tried with admin FTP
            return ReturnResult.AccessUnauthorized;
        }
        else if (fsResult == FileSystemStatusCode.InUse)
        {
            // TODO: Show inuse dialog
            //await DialogDisplayHelper.ShowDialogAsync(DynamicDialogFactory.GetFor_FileInUseDialog());
            return ReturnResult.Failed;
        }

        if (deleteFromRecycleBin)
        {
            // Recycle bin also stores a file starting with $I for each item
            var iFilePath = Path.Combine(Path.GetDirectoryName(source.Path)!, Path.GetFileName(source.Path).Replace("$R", "$I", StringComparison.Ordinal));

            await viewModel.GetFileFromPathAsync(iFilePath)
                .OnSuccess(iFile => iFile.DeleteAsync(StorageDeleteOption.PermanentDelete).AsTask());
        }

        if (fsResult)
        {
            //await viewModel.RemoveFileOrFolderAsync(source.Path);

            if (!permanently)
            {
                // Enumerate Recycle Bin
                IEnumerable<ShellFileItem> nameMatchItems, items = await RecycleBinHelpers.EnumerateRecycleBin();

                // Get name matching files
                nameMatchItems = FileExtensionHelpers.IsShortcutOrUrlFile(source.Path)
                    ? items.Where((item) => item.FilePath == Path.Combine(Path.GetDirectoryName(source.Path)!, Path.GetFileNameWithoutExtension(source.Path)))
                    : items.Where((item) => item.FilePath == source.Path);

                // Get newest file
                ShellFileItem item = nameMatchItems.OrderBy((item) => item.RecycleDate).FirstOrDefault()!;
            }

            return ReturnResult.Success;
        }
        else
        {
            // Stop at first error
            return ReturnResult.Failed;
        }
    }

    #endregion

    public void Dispose()
    {

    }
}
