// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Threading;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.ViewModels.Pages.Widget;
using Files.App.Helpers;
using Files.App.Utils.RecycleBin;
using Files.App.Utils.StatusCenter;
using Files.Core.Data.Enums;
using Files.Core.Data.Items;
using Files.Shared.Extensions;
using Files.Shared.Helpers;
using Windows.Storage;
using static DesktopWidgets3.Services.DialogService;

namespace Files.App.Utils.Storage;

public sealed class FileSystemHelpers : IFileSystemHelpers
{
    private IFileSystemOperations fileSystemOperations;

    public FileSystemHelpers()
    {
        fileSystemOperations = new ShellFileSystemOperations();
    }

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
            var dialogService = DesktopWidgets3.App.GetService<IDialogService>();

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
                fsResult = await viewModel.ItemViewModel.GetFileFromPathAsync(source.Path)
                    .OnSuccess((t) => t.DeleteAsync(permanently ? StorageDeleteOption.PermanentDelete : StorageDeleteOption.Default).AsTask());
            }
            else if (source.ItemType == FilesystemItemType.Directory)
            {
                fsResult = await viewModel.ItemViewModel.GetFolderFromPathAsync(source.Path)
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

            await viewModel.ItemViewModel.GetFileFromPathAsync(iFilePath)
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

    #region rename items

    // Her UserSettingsService.FoldersSettingsService.AreAlternateStreamsVisible = false.
    private static char[] RestrictedCharacters => new[] { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };

    private static readonly string[] RestrictedFileNames = new string[]
    {
            "CON", "PRN", "AUX",
            "NUL", "COM1", "COM2",
            "COM3", "COM4", "COM5",
            "COM6", "COM7", "COM8",
            "COM9", "LPT1", "LPT2",
            "LPT3", "LPT4", "LPT5",
            "LPT6", "LPT7", "LPT8", "LPT9"
    };

    public static string FilterRestrictedCharacters(string input)
    {
        int invalidCharIndex;
        while ((invalidCharIndex = input.IndexOfAny(RestrictedCharacters)) >= 0)
        {
            input = input.Remove(invalidCharIndex, 1);
        }
        return input;
    }

    public static bool ContainsRestrictedCharacters(string input)
    {
        return input.IndexOfAny(RestrictedCharacters) >= 0;
    }

    public async Task<ReturnResult> RenameAsync(FolderViewViewModel viewModel, IStorageItemWithPath source, string newName, NameCollisionOption collision, bool showExtensionDialog = true)
    {
        var returnStatus = ReturnResult.InProgress;
        var progress = new Progress<StatusCenterItemProgressModel>();
        progress.ProgressChanged += (s, e) => returnStatus = returnStatus < ReturnResult.Failed ? e.Status!.Value.ToStatus() : returnStatus;

        if (!IsValidForFilename(newName))
        {
            // TODO: Show error dialog
            /*await DialogDisplayHelper.ShowDialogAsync(
                "ErrorDialogThisActionCannotBeDone".GetLocalizedResource(),
                "ErrorDialogNameNotAllowed".GetLocalizedResource());*/
            return ReturnResult.Failed;
        }

        switch (source.ItemType)
        {
            case FilesystemItemType.Directory:
                returnStatus = await fileSystemOperations.RenameAsync(viewModel, source, newName, collision, progress);
                break;

            // Prompt user when extension has changed, not when file name has changed
            case FilesystemItemType.File:
                if(showExtensionDialog && Path.GetExtension(source.Path) != Path.GetExtension(newName))
                {
                    // TODO: Show dialog here!
                    //var yesSelected = await DialogDisplayHelper.ShowDialogAsync("Rename".GetLocalizedResource(), "RenameFileDialog/Text".GetLocalizedResource(), "Yes".GetLocalizedResource(), "No".GetLocalizedResource());
                    var yesSelected = true;
                    if (yesSelected)
                    {
                        returnStatus = await fileSystemOperations.RenameAsync(viewModel, source, newName, collision, progress);
                        break;
                    }

                    break;
                }

                returnStatus = await fileSystemOperations.RenameAsync(viewModel,source, newName, collision, progress);
                break;

            default:
                returnStatus = await fileSystemOperations.RenameAsync(viewModel, source, newName, collision, progress);
                break;
        }

        //await jumpListService.RemoveFolderAsync(source.Path); // Remove items from jump list

        await Task.Yield();
        return returnStatus;
    }

    public static bool IsValidForFilename(string name) => !string.IsNullOrWhiteSpace(name) && !ContainsRestrictedCharacters(name) && !ContainsRestrictedFileName(name);

    public static bool ContainsRestrictedFileName(string input)
    {
        foreach (var name in RestrictedFileNames)
        {
            if (input.StartsWith(name, StringComparison.OrdinalIgnoreCase) && (input.Length == name.Length || input[name.Length] == '.'))
            {
                return true;
            }
        }

        return false;
    }

    #endregion

    public void Dispose()
    {
        fileSystemOperations?.Dispose();
        fileSystemOperations = null!;
    }
}
