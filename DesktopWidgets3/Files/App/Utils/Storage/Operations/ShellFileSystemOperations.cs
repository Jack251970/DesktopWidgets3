// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.Data.Enums;
using Files.Core.Data.Items;
using Windows.Storage;
using Files.App.Utils.StatusCenter;
using DesktopWidgets3.ViewModels.Pages.Widget;
using Files.Shared.Extensions;
using DesktopWidgets3.Helpers;
using Files.App.Utils.RecycleBin;
using Files.Core.ViewModels.Dialogs.FileSystemDialog;
using Files.App.Helpers;
using Files.Core.ViewModels.Dialogs;

namespace Files.App.Utils.Storage;

/// <summary>
/// Provides group of shell file system operation for given page instance.
/// </summary>
public class ShellFileSystemOperations : IFileSystemOperations
{
    private FileSystemOperations _fileSystemOperations;

    public ShellFileSystemOperations()
    {
        _fileSystemOperations = new FileSystemOperations();
    }

    #region delete items

    public Task DeleteAsync(FolderViewViewModel viewModel, IStorageItem source, IProgress<StatusCenterItemProgressModel> progress, bool permanently, CancellationToken cancellationToken)
    {
        return DeleteAsync(viewModel, source.FromStorageItem(), progress, permanently, cancellationToken);
    }

    public Task DeleteAsync(FolderViewViewModel viewModel, IStorageItemWithPath source, IProgress<StatusCenterItemProgressModel> progress, bool permanently, CancellationToken cancellationToken)
    {
        return DeleteItemsAsync(viewModel, source.CreateList(), progress, permanently, cancellationToken);
    }

    public async Task DeleteItemsAsync(FolderViewViewModel viewModel, IList<IStorageItem> source, IProgress<StatusCenterItemProgressModel> progress, bool permanently, CancellationToken cancellationToken)
    {
        await DeleteItemsAsync(viewModel, await source.Select((item) => item.FromStorageItem()).ToListAsync(), progress, permanently, cancellationToken);
    }

    public async Task DeleteItemsAsync(FolderViewViewModel viewModel, IList<IStorageItemWithPath> source, IProgress<StatusCenterItemProgressModel> progress, bool permanently, CancellationToken cancellationToken, bool asAdmin = false)
    {
        if (source.Any(x => string.IsNullOrWhiteSpace(x.Path) || x.Path.StartsWith(@"\\?\", StringComparison.Ordinal) || FtpHelpers.IsFtpPath(x.Path)))
        {
            // Fallback to built-in file operations
            await _fileSystemOperations.DeleteItemsAsync(viewModel, source, progress, permanently, cancellationToken);
        }

        StatusCenterItemProgressModel fsProgress = new(
            progress,
            true,
            FileSystemStatusCode.InProgress,
            source.Count);

        fsProgress.Report();

        var deleteFilePaths = source.Select(s => s.Path).Distinct();
        var deleteFromRecycleBin = source.Any() && RecycleBinHelpers.IsPathUnderRecycleBin(source.ElementAt(0).Path);

        permanently |= deleteFromRecycleBin;

        if (deleteFromRecycleBin)
        {
            // Recycle bin also stores a file starting with $I for each item
            deleteFilePaths = deleteFilePaths.Concat(source.Select(x => Path.Combine(Path.GetDirectoryName(x.Path)!, Path.GetFileName(x.Path).Replace("$R", "$I", StringComparison.Ordinal)))).Distinct();
        }

        var operationID = Guid.NewGuid().ToString();
        using var r = cancellationToken.Register(CancelOperation!, operationID, false);

        var (success, response) = await FileOperationsHelpers.DeleteItemAsync(deleteFilePaths.ToArray(), permanently, viewModel.WidgetWindow.WindowHandle.ToInt64(), asAdmin, progress, operationID);

        var result = (FilesystemResult)success;
        var deleteResult = new ShellOperationResult();
        deleteResult.Items.AddRange(response?.Final ?? Enumerable.Empty<ShellOperationItemResult>());

        result &= (FilesystemResult)deleteResult.Items.All(x => x.Succeeded);

        if (result)
        {
            fsProgress.ReportStatus(FileSystemStatusCode.Success);

            foreach (var item in deleteResult.Items)
            {
                await viewModel.ItemViewModel.RemoveFileOrFolderAsync(item.Source);
            }

            /*var recycledSources = deleteResult.Items.Where(x => x.Succeeded && x.Destination is not null && x.Source != x.Destination);
            if (recycledSources.Any())
            {
                var sourceMatch = await recycledSources.Select(x => source.DistinctBy(x => x.Path)
                    .SingleOrDefault(s => s.Path.Equals(x.Source, StringComparison.OrdinalIgnoreCase))).Where(x => x is not null).ToListAsync();
            }*/
        }
        else
        {
            if (deleteResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.Unauthorized))
            {
                if (!asAdmin && await RequestAdminOperation(viewModel))
                {
                    await DeleteItemsAsync(viewModel, source, progress, permanently, cancellationToken, true);
                }
            }
            else if (deleteResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.InUse))
            {
                var failedSources = deleteResult.Items.Where(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.InUse);
                var filePath = failedSources.Select(x => x.Source); // When deleting only source can be in use but shell returns COPYENGINE_E_SHARING_VIOLATION_DEST for folders
                var lockingProcess = WhoIsLocking(filePath);

                if (await GetFileInUseDialog(viewModel, filePath, lockingProcess) == DialogResult.Primary)
                {
                    var retrySource = await failedSources.Select(x => source.DistinctBy(x => x.Path).SingleOrDefault(s => s.Path == x.Source)).Where(x => x is not null).ToListAsync();
                    await DeleteItemsAsync(viewModel, retrySource!, progress, permanently, cancellationToken);
                }
            }
            else if (deleteResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.NameTooLong))
            {
                // Abort, path is too long for recycle bin
            }
            else if (deleteResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.NotFound))
            {
                await DialogDisplayHelper.ShowDialogAsync(viewModel, "FileNotFoundDialog/Title".GetLocalized(), "FileNotFoundDialog/Text".GetLocalized());
            }
            else if (deleteResult.Items.All(x => x.HResult == -1) && permanently) // ADS
            {
                // Retry with StorageFile API
                var failedSources = deleteResult.Items.Where(x => !x.Succeeded);
                var sourceMatch = await failedSources.Select(x => source.DistinctBy(x => x.Path).SingleOrDefault(s => s.Path.Equals(x.Source, StringComparison.OrdinalIgnoreCase))).Where(x => x is not null).ToListAsync();

                await _fileSystemOperations.DeleteItemsAsync(viewModel, sourceMatch!, progress, permanently, cancellationToken);
            }

            fsProgress.ReportStatus(CopyEngineResult.Convert(deleteResult.Items.FirstOrDefault(x => !x.Succeeded)?.HResult));
        }
    }

    #endregion

    #region rename items

    public async Task RenameAsync(FolderViewViewModel viewModel, IStorageItemWithPath source, string newName, NameCollisionOption collision, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken, bool asAdmin = false)
    {
        if (string.IsNullOrWhiteSpace(source.Path) || source.Path.StartsWith(@"\\?\", StringComparison.Ordinal) || FtpHelpers.IsFtpPath(source.Path) || ZipStorageFolder.IsZipPath(source.Path, false))
        {
            // Fallback to built-in file operations
            await _fileSystemOperations.RenameAsync(viewModel, source, newName, collision, progress, cancellationToken);
        }

        StatusCenterItemProgressModel fsProgress = new(progress, true, FileSystemStatusCode.InProgress);
        fsProgress.Report();

        var renameResult = new ShellOperationResult();
        var (status, response) = await FileOperationsHelpers.RenameItemAsync(source.Path, newName, collision == NameCollisionOption.ReplaceExisting, viewModel.WidgetWindow.WindowHandle.ToInt64(), asAdmin);
        var result = (FilesystemResult)status;

        renameResult.Items.AddRange(response?.Final ?? Enumerable.Empty<ShellOperationItemResult>());

        result &= (FilesystemResult)renameResult.Items.All(x => x.Succeeded);

        if (result)
        {
            fsProgress.ReportStatus(FileSystemStatusCode.Success);

            var renamedSources = renameResult.Items.Where(x => x.Succeeded && x.Destination is not null && x.Source != x.Destination)
                .Where(x => x.Source == source.Path);
            if (renamedSources.Any())
            {
                return;
            }

            // Cannot undo overwrite operation
            fsProgress.ReportStatus(FileSystemStatusCode.AlreadyExists);
        }
        else
        {
            if (renameResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.Unauthorized))
            {
                if (!asAdmin && await RequestAdminOperation(viewModel))
                {
                    await RenameAsync(viewModel, source, newName, collision, progress, cancellationToken, true);
                }
            }
            else if (renameResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.InUse))
            {
                var failedSources = renameResult.Items.Where(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.InUse);
                var filePath = failedSources.Select(x => x.HResult == CopyEngineResult.COPYENGINE_E_SHARING_VIOLATION_SRC ? x.Source : x.Destination);
                var lockingProcess = WhoIsLocking(filePath);

                if (await GetFileInUseDialog(viewModel, filePath, lockingProcess) == DialogResult.Primary)
                {
                    await RenameAsync(viewModel, source, newName, collision, progress, cancellationToken);
                }
            }
            else if (renameResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.NameTooLong))
            {
                // Retry with the StorageFile API
                await _fileSystemOperations.RenameAsync(viewModel, source, newName, collision, progress, cancellationToken);
            }
            else if (renameResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.NotFound))
            {
                await DialogDisplayHelper.ShowDialogAsync(viewModel, "RenameError/ItemDeleted/Title".GetLocalized(), "RenameError/ItemDeleted/Text".GetLocalized());
            }
            else if (renameResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.AlreadyExists))
            {
                await DialogDisplayHelper.ShowDialogAsync(viewModel, "ItemAlreadyExistsDialogTitle".GetLocalized(), "ItemAlreadyExistsDialogContent".GetLocalized());
            }
            // ADS
            else if (renameResult.Items.All(x => x.HResult == -1))
            {
                // Retry with StorageFile API
                await _fileSystemOperations.RenameAsync(viewModel, source, newName, collision, progress, cancellationToken);
            }

            fsProgress.ReportStatus(CopyEngineResult.Convert(renameResult.Items.FirstOrDefault(x => !x.Succeeded)?.HResult));
        }
    }

    #endregion

    #region dialogs

    private async Task<bool> RequestAdminOperation(FolderViewViewModel viewModel)
    {
        var dialogService = viewModel.DialogService;
        return await dialogService.ShowDialogAsync(new ElevateConfirmDialogViewModel()) == DialogResult.Primary;
    }

    private Task<DialogResult> GetFileInUseDialog(FolderViewViewModel viewModel, IEnumerable<string> source, IEnumerable<Win32Process> lockingProcess = null!)
    {
        var titleText = "FileInUseDialog/Title".GetLocalized();
        var subtitleText = lockingProcess.IsEmpty()
            ? "FileInUseDialog/Text".GetLocalized()
            : string.Format("FileInUseByDialog/Text".GetLocalized(), string.Join(", ", lockingProcess.Select(x => $"{x.AppName ?? x.Name} (PID: {x.Pid})")));

        return GetFileListDialog(viewModel, source, titleText, subtitleText, "Retry".GetLocalized(), "Cancel".GetLocalized());
    }

    private async Task<DialogResult> GetFileListDialog(FolderViewViewModel viewModel, IEnumerable<string> source, string titleText, string descriptionText, string primaryButtonText, string secondaryButtonText)
    {
        var incomingItems = new List<BaseFileSystemDialogItemViewModel>();
        List<ShellFileItem> binItems = null!;
        foreach (var src in source)
        {
            if (RecycleBinHelpers.IsPathUnderRecycleBin(src))
            {
                binItems ??= await RecycleBinHelpers.EnumerateRecycleBin();

                // Might still be null because we're deserializing the list from Json
                if (!binItems.IsEmpty())
                {
                    // Get original file name
                    var matchingItem = binItems.FirstOrDefault(x => x.RecyclePath == src);

                    incomingItems.Add(new FileSystemDialogDefaultItemViewModel() { SourcePath = src, DisplayName = matchingItem?.FileName });
                }
            }
            else
            {
                incomingItems.Add(new FileSystemDialogDefaultItemViewModel() { SourcePath = src });
            }
        }

        var dialogViewModel = FileSystemDialogViewModel.GetDialogViewModel(
            viewModel, incomingItems, titleText, descriptionText, primaryButtonText, secondaryButtonText);

        var dialogService = viewModel.DialogService;

        return await dialogService.ShowDialogAsync(dialogViewModel);
    }

    #endregion

    #region check file in use

    private IEnumerable<Win32Process> WhoIsLocking(IEnumerable<string> filesToCheck)
    {
        return FileOperationsHelpers.CheckFileInUse(filesToCheck.ToArray())!;
    }

    #endregion

    #region cancel operation

    private void CancelOperation(object operationID)
    {
        FileOperationsHelpers.TryCancelOperation((string)operationID);
    }

    #endregion

    public void Dispose()
    {
        _fileSystemOperations?.Dispose();
        _fileSystemOperations = null!;
    }
}
