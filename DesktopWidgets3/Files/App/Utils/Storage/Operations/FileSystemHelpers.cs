// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.Helpers;
using DesktopWidgets3.ViewModels.Pages.Widget;
using Files.App.Helpers;
using Files.App.Utils.RecycleBin;
using Files.App.Utils.StatusCenter;
using Files.Core.Data.Enums;
using Files.Core.Data.Items;
using Files.Core.ViewModels.Dialogs.FileSystemDialog;
using Files.Shared.Extensions;
using Windows.Storage;

namespace Files.App.Utils.Storage;

public sealed class FileSystemHelpers : IFileSystemHelpers
{
    private IFileSystemOperations fileSystemOperations;

    private readonly CancellationToken cancellationToken = CancellationToken.None;

    public FileSystemHelpers()
    {
        fileSystemOperations = new ShellFileSystemOperations();
    }

    #region Delete

    public async Task<ReturnResult> DeleteItemsAsync(FolderViewViewModel viewModel, IEnumerable<IStorageItemWithPath> source, DeleteConfirmationPolicies showDialog, bool permanently)
    {
        source = await source.ToListAsync();

        var returnStatus = ReturnResult.InProgress;

        var progress = new Progress<StatusCenterItemProgressModel>();
        progress.ProgressChanged += (s, e) => returnStatus = returnStatus < ReturnResult.Failed ? e.Status!.Value.ToStatus() : returnStatus;

        var deleteFromRecycleBin = source.Select(item => item.Path).Any(RecycleBinHelpers.IsPathUnderRecycleBin);
        var canBeSentToBin = !deleteFromRecycleBin && await RecycleBinHelpers.HasRecycleBin(source.FirstOrDefault()?.Path);

        // Get the delete type from the dialog
        if (showDialog is DeleteConfirmationPolicies.Always ||
            showDialog is DeleteConfirmationPolicies.PermanentOnly &&
            (permanently || !canBeSentToBin))
        {
            var incomingItems = new List<BaseFileSystemDialogItemViewModel>();
            List<ShellFileItem>? binItems = null;

            foreach (var src in source)
            {
                if (RecycleBinHelpers.IsPathUnderRecycleBin(src.Path))
                {
                    binItems ??= await RecycleBinHelpers.EnumerateRecycleBin();

                    // Might still be null because we're deserializing the list from Json
                    if (!binItems.IsEmpty())
                    {
                        // Get original file name
                        var matchingItem = binItems.FirstOrDefault(x => x.RecyclePath == src.Path);
                        incomingItems.Add(new FileSystemDialogDefaultItemViewModel() { SourcePath = src.Path, DisplayName = matchingItem?.FileName ?? src.Name });
                    }
                }
                else
                {
                    incomingItems.Add(new FileSystemDialogDefaultItemViewModel() { SourcePath = src.Path });
                }
            }

            var dialogViewModel = FileSystemDialogViewModel.GetDialogViewModel(
                viewModel,
                new() { IsInDeleteMode = true },
                (!canBeSentToBin || permanently, canBeSentToBin),
                FileSystemOperationType.Delete,
                incomingItems,
                new());

            var dialogService = viewModel.DialogService;

            // Return if the result isn't delete
            if (await dialogService.ShowDialogAsync(dialogViewModel) != DialogResult.Primary)
            {
                return ReturnResult.Cancelled;
            }

            // Delete selected items if the result is Yes
            permanently = dialogViewModel.DeletePermanently;
        }
        else
        {
            // Delete permanently if recycle bin is not supported
            permanently |= !canBeSentToBin;
        }

        // Delete file
        await fileSystemOperations.DeleteItemsAsync(viewModel, (IList<IStorageItemWithPath>)source, progress, permanently, cancellationToken);
        
        return returnStatus;
    }

    public Task<ReturnResult> DeleteItemAsync(FolderViewViewModel viewModel, IStorageItemWithPath source, DeleteConfirmationPolicies showDialog, bool permanently)
            => DeleteItemsAsync(viewModel, source.CreateEnumerable(), showDialog, permanently);

    public Task<ReturnResult> DeleteItemsAsync(FolderViewViewModel viewModel, IEnumerable<IStorageItem> source, DeleteConfirmationPolicies showDialog, bool permanently)
        => DeleteItemsAsync(viewModel, source.Select((item) => item.FromStorageItem()), showDialog, permanently);

    public Task<ReturnResult> DeleteItemAsync(FolderViewViewModel viewModel, IStorageItem source, DeleteConfirmationPolicies showDialog, bool permanently)
        => DeleteItemAsync(viewModel, source.FromStorageItem(), showDialog, permanently);

    #endregion

    #region Rename

    public async Task<ReturnResult> RenameAsync(FolderViewViewModel viewModel, IStorageItemWithPath source, string newName, NameCollisionOption collision, bool showExtensionDialog = true)
    {
        var returnStatus = ReturnResult.InProgress;
        var progress = new Progress<StatusCenterItemProgressModel>();
        progress.ProgressChanged += (s, e) => returnStatus = returnStatus < ReturnResult.Failed ? e.Status!.Value.ToStatus() : returnStatus;

        if (!IsValidForFilename(newName))
        {
            await DialogDisplayHelper.ShowDialogAsync(
                viewModel,
                "ErrorDialogThisActionCannotBeDone".GetLocalized(),
                "ErrorDialogNameNotAllowed".GetLocalized());
            return ReturnResult.Failed;
        }

        switch (source.ItemType)
        {
            case FilesystemItemType.Directory:
                await fileSystemOperations.RenameAsync(viewModel, source, newName, collision, progress, cancellationToken);
                break;

            // Prompt user when extension has changed, not when file name has changed
            case FilesystemItemType.File:
                if(showExtensionDialog && Path.GetExtension(source.Path) != Path.GetExtension(newName))
                {
                    var yesSelected = await DialogDisplayHelper.ShowDialogAsync(viewModel, "Rename".GetLocalized(), "RenameFileDialog/Text".GetLocalized(), "Yes".GetLocalized(), "No".GetLocalized());
                    if (yesSelected)
                    {
                        await fileSystemOperations.RenameAsync(viewModel, source, newName, collision, progress, cancellationToken);
                        break;
                    }

                    break;
                }

                await fileSystemOperations.RenameAsync(viewModel,source, newName, collision, progress, cancellationToken);
                break;

            default:
                await fileSystemOperations.RenameAsync(viewModel, source, newName, collision, progress, cancellationToken);
                break;
        }

        //await jumpListService.RemoveFolderAsync(source.Path); // Remove items from jump list

        await Task.Yield();
        return returnStatus;
    }

    public Task<ReturnResult> RenameAsync(FolderViewViewModel viewModel, IStorageItem source, string newName, NameCollisionOption collision, bool showExtensionDialog = true)
            => RenameAsync(viewModel, source.FromStorageItem(), newName, collision, showExtensionDialog);

    #endregion

    #region Static Methods

    // TODO: Here UserSettingsService.FoldersSettingsService.AreAlternateStreamsVisible = false.
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
