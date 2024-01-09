// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.Files.Core.Data.Enums;
using DesktopWidgets3.Files.Core.Data.Items;
using Windows.Storage;
using DesktopWidgets3.Files.App.Utils.StatusCenter;
using DesktopWidgets3.ViewModels.Pages.Widget;
using DesktopWidgets3.Files.Shared.Extensions;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.Files.App.Utils.RecycleBin;
using DesktopWidgets3.Files.Core.ViewModels.Dialogs.FileSystemDialog;
using DesktopWidgets3.Files.App.Helpers;
using DesktopWidgets3.Files.Core.ViewModels.Dialogs;
using DesktopWidgets3.Files.App.Extensions;

namespace DesktopWidgets3.Files.App.Utils.Storage;

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
                await viewModel.FileSystemViewModel.RemoveFileOrFolderAsync(item.Source);
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

    #region create shortcuts

    public async Task CreateShortcutItemsAsync(FolderViewViewModel viewModel, IList<IStorageItemWithPath> source, IList<string> destination, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken)
    {
        var createdSources = new List<IStorageItemWithPath>();
        var createdDestination = new List<IStorageItemWithPath>();

        StatusCenterItemProgressModel fsProgress = new(progress, true, FileSystemStatusCode.InProgress, source.Count);
        fsProgress.Report();

        var items = source.Zip(destination, (src, dest) => new { src, dest }).Where(x => !string.IsNullOrEmpty(x.src.Path) && !string.IsNullOrEmpty(x.dest));
        foreach (var item in items)
        {
            var result = await FileOperationsHelpers.CreateOrUpdateLinkAsync(item.dest, item.src.Path);

            if (!result)
            {
                result = await UIFileSystemHelpers.HandleShortcutCannotBeCreated(viewModel, Path.GetFileName(item.dest), item.src.Path);
            }

            if (result)
            {
                createdSources.Add(item.src);
                createdDestination.Add(StorageHelpers.FromPathAndType(item.dest, FilesystemItemType.File));
            }

            fsProgress.AddProcessedItemsCount(1);
            fsProgress.Report();
        }

        fsProgress.ReportStatus(createdSources.Count == source.Count ? FileSystemStatusCode.Success : FileSystemStatusCode.Generic);
    }

    #endregion

    #region copy items

    public Task CopyAsync(FolderViewViewModel viewModel, IStorageItem source, string destination, NameCollisionOption collision, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken)
    {
        return CopyAsync(viewModel, source.FromStorageItem(), destination, collision, progress, cancellationToken);
    }

    public Task CopyAsync(FolderViewViewModel viewModel, IStorageItemWithPath source, string destination, NameCollisionOption collision, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken)
    {
        return CopyItemsAsync(viewModel, source.CreateList(), destination.CreateList(), collision.ConvertBack().CreateList(), progress, cancellationToken);
    }

    public async Task CopyItemsAsync(FolderViewViewModel viewModel, IList<IStorageItem> source, IList<string> destination, IList<FileNameConflictResolveOptionType> collisions, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken)
    {
        await CopyItemsAsync(viewModel, await source.Select((item) => item.FromStorageItem()).ToListAsync(), destination, collisions, progress, cancellationToken);
    }

    public async Task CopyItemsAsync(FolderViewViewModel viewModel, IList<IStorageItemWithPath> source, IList<string> destination, IList<FileNameConflictResolveOptionType> collisions, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken, bool asAdmin = false)
    {
        if (source.Any(x => string.IsNullOrWhiteSpace(x.Path) || x.Path.StartsWith(@"\\?\", StringComparison.Ordinal)) || destination.Any(x => string.IsNullOrWhiteSpace(x) || x.StartsWith(@"\\?\", StringComparison.Ordinal) || FtpHelpers.IsFtpPath(x) || ZipStorageFolder.IsZipPath(x, false)))
        {
            // Fallback to built-in file operations
            await _fileSystemOperations.CopyItemsAsync(viewModel, source, destination, collisions, progress, cancellationToken);
            
            return;
        }

        StatusCenterItemProgressModel fsProgress = new(
            progress,
            true,
            FileSystemStatusCode.InProgress,
            source.Count);

        fsProgress.Report();

        var sourceNoSkip = source.Zip(collisions, (src, coll) => new { src, coll }).Where(item => item.coll != FileNameConflictResolveOptionType.Skip).Select(item => item.src);
        var destinationNoSkip = destination.Zip(collisions, (src, coll) => new { src, coll }).Where(item => item.coll != FileNameConflictResolveOptionType.Skip).Select(item => item.src);
        var collisionsNoSkip = collisions.Where(c => c != FileNameConflictResolveOptionType.Skip);

        var operationID = Guid.NewGuid().ToString();

        using var r = cancellationToken.Register(CancelOperation!, operationID, false);

        var sourceReplace = sourceNoSkip.Zip(collisionsNoSkip, (src, coll) => new { src, coll }).Where(item => item.coll == FileNameConflictResolveOptionType.ReplaceExisting).Select(item => item.src);
        var destinationReplace = destinationNoSkip.Zip(collisionsNoSkip, (src, coll) => new { src, coll }).Where(item => item.coll == FileNameConflictResolveOptionType.ReplaceExisting).Select(item => item.src);
        var sourceRename = sourceNoSkip.Zip(collisionsNoSkip, (src, coll) => new { src, coll }).Where(item => item.coll != FileNameConflictResolveOptionType.ReplaceExisting).Select(item => item.src);
        var destinationRename = destinationNoSkip.Zip(collisionsNoSkip, (src, coll) => new { src, coll }).Where(item => item.coll != FileNameConflictResolveOptionType.ReplaceExisting).Select(item => item.src);

        var result = (FilesystemResult)true;
        var copyResult = new ShellOperationResult();

        if (sourceRename.Any())
        {
            var resultItem = await FileOperationsHelpers.CopyItemAsync(sourceRename.Select(s => s.Path).ToArray(), destinationRename.ToArray(), false, viewModel.WidgetWindow.WindowHandle.ToInt64(), asAdmin, progress, operationID);

            result &= (FilesystemResult)resultItem.Item1;

            copyResult.Items.AddRange(resultItem.Item2?.Final ?? Enumerable.Empty<ShellOperationItemResult>());
        }

        if (sourceReplace.Any())
        {
            var resultItem = await FileOperationsHelpers.CopyItemAsync(sourceReplace.Select(s => s.Path).ToArray(), destinationReplace.ToArray(), true, viewModel.WidgetWindow.WindowHandle.ToInt64(), asAdmin, progress, operationID);

            result &= (FilesystemResult)resultItem.Item1;

            copyResult.Items.AddRange(resultItem.Item2?.Final ?? Enumerable.Empty<ShellOperationItemResult>());
        }

        result &= (FilesystemResult)copyResult.Items.All(x => x.Succeeded);

        if (result)
        {
            fsProgress.ReportStatus(FileSystemStatusCode.Success);
            var copiedSources = copyResult.Items.Where(x => x.Succeeded && x.Destination is not null && x.Source != x.Destination);
            if (copiedSources.Any())
            {
                var sourceMatch = await copiedSources.Select(x => sourceRename
                    .SingleOrDefault(s => s.Path.Equals(x.Source, StringComparison.OrdinalIgnoreCase))).Where(x => x is not null).ToListAsync();

                return;
            }

            // Cannot undo overwrite operation
            return;
        }
        else
        {
            if (copyResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.Unauthorized))
            {
                if (!asAdmin && await RequestAdminOperation(viewModel))
                {
                    await CopyItemsAsync(viewModel, source, destination, collisions, progress, cancellationToken, true);

                    return;
                }
            }
            else if (copyResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.InUse))
            {
                var failedSources = copyResult.Items.Where(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.InUse);
                var filePath = failedSources.Select(x => x.HResult == CopyEngineResult.COPYENGINE_E_SHARING_VIOLATION_SRC ? x.Source : x.Destination);
                var lockingProcess = WhoIsLocking(filePath);

                switch (await GetFileInUseDialog(viewModel, filePath, lockingProcess))
                {
                    case DialogResult.Primary:
                        var copyZip = sourceNoSkip.Zip(destinationNoSkip, (src, dest) => new { src, dest }).Zip(collisionsNoSkip, (z1, coll) => new { z1.src, z1.dest, coll });
                        var sourceMatch = await failedSources.Select(x => copyZip.SingleOrDefault(s => s.src.Path.Equals(x.Source, StringComparison.OrdinalIgnoreCase))).Where(x => x is not null).ToListAsync();

                        return;
                }
            }
            else if (copyResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.NameTooLong))
            {
                // Retry with the StorageFile API
                var failedSources = copyResult.Items.Where(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.NameTooLong);
                var copyZip = sourceNoSkip.Zip(destinationNoSkip, (src, dest) => new { src, dest }).Zip(collisionsNoSkip, (z1, coll) => new { z1.src, z1.dest, coll });
                var sourceMatch = await failedSources.Select(x => copyZip.SingleOrDefault(s => s.src.Path.Equals(x.Source, StringComparison.OrdinalIgnoreCase))).Where(x => x is not null).ToListAsync();

                await _fileSystemOperations.CopyItemsAsync(
                    viewModel,
                    await sourceMatch.Select(x => x!.src).ToListAsync(),
                    await sourceMatch.Select(x => x!.dest).ToListAsync(),
                    await sourceMatch.Select(x => x!.coll).ToListAsync(), progress, cancellationToken);

                return;
            }
            else if (copyResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.NotFound))
            {
                await DialogDisplayHelper.ShowDialogAsync(viewModel, "FileNotFoundDialog/Title".GetLocalized(), "FileNotFoundDialog/Text".GetLocalized());
            }
            else if (copyResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.AlreadyExists))
            {
                await DialogDisplayHelper.ShowDialogAsync(viewModel, "ItemAlreadyExistsDialogTitle".GetLocalized(), "ItemAlreadyExistsDialogContent".GetLocalized());
            }
            else if (copyResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.PropertyLoss))
            {
                var failedSources = copyResult.Items.Where(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.PropertyLoss);
                var filePath = failedSources.Select(x => x.Source);

                switch (await GetFileListDialog(viewModel, filePath, "FilePropertiesCannotBeCopied".GetLocalized(), "CopyFileWithoutProperties".GetLocalized(), "OK".GetLocalized(), "Cancel".GetLocalized()))
                {
                    case DialogResult.Primary:
                        var copyZip = sourceNoSkip.Zip(destinationNoSkip, (src, dest) => new { src, dest }).Zip(collisionsNoSkip, (z1, coll) => new { z1.src, z1.dest, coll });
                        var sourceMatch = await failedSources.Select(x => copyZip.SingleOrDefault(s => s.src.Path.Equals(x.Source, StringComparison.OrdinalIgnoreCase))).Where(x => x is not null).ToListAsync();

                        await CopyItemsAsync(
                            viewModel,
                            await sourceMatch.Select(x => x!.src).ToListAsync(),
                            await sourceMatch.Select(x => x!.dest).ToListAsync(),
                            // Force collision option to "replace" to accept copying with property loss
                            // Ok since property loss error is raised after checking if the destination already exists
                            await sourceMatch.Select(x => FileNameConflictResolveOptionType.ReplaceExisting).ToListAsync(), progress, cancellationToken);

                        return;
                }
            }
            else if (copyResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.FileTooLarge))
            {
                var failingItems = copyResult.Items
                    .Where(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.FileTooLarge)
                    .Select(item => item.Source);

                await viewModel.DialogService.ShowDialogAsync(new FileTooLargeDialogViewModel(failingItems));
            }
            // ADS
            else if (copyResult.Items.All(x => x.HResult == -1))
            {
                // Retry with the StorageFile API
                var failedSources = copyResult.Items.Where(x => !x.Succeeded);
                var copyZip = sourceNoSkip.Zip(destinationNoSkip, (src, dest) => new { src, dest }).Zip(collisionsNoSkip, (z1, coll) => new { z1.src, z1.dest, coll });
                var sourceMatch = await failedSources.Select(x => copyZip.SingleOrDefault(s => s.src.Path.Equals(x.Source, StringComparison.OrdinalIgnoreCase))).Where(x => x is not null).ToListAsync();

                await _fileSystemOperations.CopyItemsAsync(
                    viewModel,
                    await sourceMatch.Select(x => x!.src).ToListAsync(),
                    await sourceMatch.Select(x => x!.dest).ToListAsync(),
                    await sourceMatch.Select(x => x!.coll).ToListAsync(), progress, cancellationToken);

                return;
            }

            fsProgress.ReportStatus(CopyEngineResult.Convert(copyResult.Items.FirstOrDefault(x => !x.Succeeded)?.HResult));
        }
    }

    #endregion

    #region move items

    public Task MoveAsync(FolderViewViewModel viewModel, IStorageItem source, string destination, NameCollisionOption collision, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken)
    {
        return MoveAsync(viewModel, source.FromStorageItem(), destination, collision, progress, cancellationToken);
    }

    public Task MoveAsync(FolderViewViewModel viewModel, IStorageItemWithPath source, string destination, NameCollisionOption collision, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken)
    {
        return MoveItemsAsync(viewModel, source.CreateList(), destination.CreateList(), collision.ConvertBack().CreateList(), progress, cancellationToken);
    }

    public async Task MoveItemsAsync(FolderViewViewModel viewModel, IList<IStorageItem> source, IList<string> destination, IList<FileNameConflictResolveOptionType> collisions, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken)
    {
        await MoveItemsAsync(viewModel, await source.Select((item) => item.FromStorageItem()).ToListAsync(), destination, collisions, progress, cancellationToken);
    }

    public async Task MoveItemsAsync(FolderViewViewModel viewModel, IList<IStorageItemWithPath> source, IList<string> destination, IList<FileNameConflictResolveOptionType> collisions, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken, bool asAdmin = false)
    {
        if (source.Any(x => string.IsNullOrWhiteSpace(x.Path) || x.Path.StartsWith(@"\\?\", StringComparison.Ordinal)) || destination.Any(x => string.IsNullOrWhiteSpace(x) || x.StartsWith(@"\\?\", StringComparison.Ordinal) || FtpHelpers.IsFtpPath(x) || ZipStorageFolder.IsZipPath(x, false)))
        {
            // Fallback to built-in file operations
            await _fileSystemOperations.MoveItemsAsync(viewModel, source, destination, collisions, progress, cancellationToken);

            return;
        }

        StatusCenterItemProgressModel fsProgress = new(
            progress,
            true,
            FileSystemStatusCode.InProgress,
            source.Count);

        fsProgress.Report();

        var sourceNoSkip = source.Zip(collisions, (src, coll) => new { src, coll }).Where(item => item.coll != FileNameConflictResolveOptionType.Skip).Select(item => item.src);
        var destinationNoSkip = destination.Zip(collisions, (src, coll) => new { src, coll }).Where(item => item.coll != FileNameConflictResolveOptionType.Skip).Select(item => item.src);
        var collisionsNoSkip = collisions.Where(c => c != FileNameConflictResolveOptionType.Skip);
        var operationID = Guid.NewGuid().ToString();

        using var r = cancellationToken.Register(CancelOperation!, operationID, false);

        var sourceReplace = sourceNoSkip.Zip(collisionsNoSkip, (src, coll) => new { src, coll }).Where(item => item.coll == FileNameConflictResolveOptionType.ReplaceExisting).Select(item => item.src);
        var destinationReplace = destinationNoSkip.Zip(collisionsNoSkip, (src, coll) => new { src, coll }).Where(item => item.coll == FileNameConflictResolveOptionType.ReplaceExisting).Select(item => item.src);
        var sourceRename = sourceNoSkip.Zip(collisionsNoSkip, (src, coll) => new { src, coll }).Where(item => item.coll != FileNameConflictResolveOptionType.ReplaceExisting).Select(item => item.src);
        var destinationRename = destinationNoSkip.Zip(collisionsNoSkip, (src, coll) => new { src, coll }).Where(item => item.coll != FileNameConflictResolveOptionType.ReplaceExisting).Select(item => item.src);
        var result = (FilesystemResult)true;
        var moveResult = new ShellOperationResult();

        if (sourceRename.Any())
        {
            var (status, response) = await FileOperationsHelpers.MoveItemAsync(sourceRename.Select(s => s.Path).ToArray(), destinationRename.ToArray(), false, viewModel.WidgetWindow.WindowHandle.ToInt64(), asAdmin, progress, operationID);

            result &= (FilesystemResult)status;
            moveResult.Items.AddRange(response?.Final ?? Enumerable.Empty<ShellOperationItemResult>());
        }

        if (sourceReplace.Any())
        {
            var (status, response) = await FileOperationsHelpers.MoveItemAsync(sourceReplace.Select(s => s.Path).ToArray(), destinationReplace.ToArray(), true, viewModel.WidgetWindow.WindowHandle.ToInt64(), asAdmin, progress, operationID);

            result &= (FilesystemResult)status;
            moveResult.Items.AddRange(response?.Final ?? Enumerable.Empty<ShellOperationItemResult>());
        }

        result &= (FilesystemResult)moveResult.Items.All(x => x.Succeeded);

        if (result)
        {
            fsProgress.ReportStatus(FileSystemStatusCode.Success);

            var movedSources = moveResult.Items.Where(x => x.Succeeded && x.Destination is not null && x.Source != x.Destination);
            if (movedSources.Any())
            {
                var sourceMatch = await movedSources.Select(x => sourceRename
                    .SingleOrDefault(s => s.Path.Equals(x.Source, StringComparison.OrdinalIgnoreCase))).Where(x => x is not null).ToListAsync();

                return;
            }

            // Cannot undo overwrite operation
            return;
        }
        else
        {
            fsProgress.ReportStatus(CopyEngineResult.Convert(moveResult.Items.FirstOrDefault(x => !x.Succeeded)?.HResult));
            if (moveResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.Unauthorized))
            {
                if (!asAdmin && await RequestAdminOperation(viewModel))
                {
                    await MoveItemsAsync(viewModel, source, destination, collisions, progress, cancellationToken, true);

                    return;
                }
            }
            else if (source.Zip(destination, (src, dest) => (src, dest)).FirstOrDefault(x => x.src.ItemType == FilesystemItemType.Directory && PathNormalization.GetParentDir(x.dest).IsSubPathOf(x.src.Path)) is (IStorageItemWithPath, string) subtree)
            {
                var destName = subtree.dest.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).Last();
                var srcName = subtree.src.Path.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).Last();

                await DialogDisplayHelper.ShowDialogAsync(viewModel, "ErrorDialogThisActionCannotBeDone".GetLocalized(), $"{"ErrorDialogTheDestinationFolder".GetLocalized()} ({destName}) {"ErrorDialogIsASubfolder".GetLocalized()} ({srcName})");
            }
            else if (moveResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.InUse))
            {
                var failedSources = moveResult.Items.Where(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.InUse);
                var filePath = failedSources.Select(x => x.HResult == CopyEngineResult.COPYENGINE_E_SHARING_VIOLATION_SRC ? x.Source : x.Destination);
                var lockingProcess = WhoIsLocking(filePath);

                switch (await GetFileInUseDialog(viewModel, filePath, lockingProcess))
                {
                    case DialogResult.Primary:
                        var moveZip = sourceNoSkip.Zip(destinationNoSkip, (src, dest) => new { src, dest }).Zip(collisionsNoSkip, (z1, coll) => new { z1.src, z1.dest, coll });
                        var sourceMatch = await failedSources.Select(x => moveZip.SingleOrDefault(s => s.src.Path.Equals(x.Source, StringComparison.OrdinalIgnoreCase))).Where(x => x is not null).ToListAsync();

                        return;
                }
            }
            else if (moveResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.NameTooLong))
            {
                // Retry with the StorageFile API
                var failedSources = moveResult.Items.Where(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.NameTooLong);
                var moveZip = sourceNoSkip.Zip(destinationNoSkip, (src, dest) => new { src, dest }).Zip(collisionsNoSkip, (z1, coll) => new { z1.src, z1.dest, coll });
                var sourceMatch = await failedSources.Select(x => moveZip.SingleOrDefault(s => s.src.Path.Equals(x.Source, StringComparison.OrdinalIgnoreCase))).Where(x => x is not null).ToListAsync();

                await _fileSystemOperations.MoveItemsAsync(
                    viewModel,
                    await sourceMatch.Select(x => x!.src).ToListAsync(),
                    await sourceMatch.Select(x => x!.dest).ToListAsync(),
                    await sourceMatch.Select(x => x!.coll).ToListAsync(), progress, cancellationToken);

                return;
            }
            else if (moveResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.NotFound))
            {
                await DialogDisplayHelper.ShowDialogAsync(viewModel, "FileNotFoundDialog/Title".GetLocalized(), "FileNotFoundDialog/Text".GetLocalized());
            }
            else if (moveResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.AlreadyExists))
            {
                await DialogDisplayHelper.ShowDialogAsync(viewModel, "ItemAlreadyExistsDialogTitle".GetLocalized(), "ItemAlreadyExistsDialogContent".GetLocalized());
            }
            else if (moveResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.PropertyLoss))
            {
                var failedSources = moveResult.Items.Where(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.PropertyLoss);
                var filePath = failedSources.Select(x => x.Source);
                switch (await GetFileListDialog(viewModel, filePath, "FilePropertiesCannotBeMoved".GetLocalized(), "MoveFileWithoutProperties".GetLocalized(), "OK".GetLocalized(), "Cancel".GetLocalized()))
                {
                    case DialogResult.Primary:
                        var copyZip = sourceNoSkip.Zip(destinationNoSkip, (src, dest) => new { src, dest }).Zip(collisionsNoSkip, (z1, coll) => new { z1.src, z1.dest, coll });
                        var sourceMatch = await failedSources.Select(x => copyZip.SingleOrDefault(s => s.src.Path.Equals(x.Source, StringComparison.OrdinalIgnoreCase))).Where(x => x is not null).ToListAsync();
                        await CopyItemsAsync(
                            viewModel,
                            await sourceMatch.Select(x => x!.src).ToListAsync(),
                            await sourceMatch.Select(x => x!.dest).ToListAsync(),
                            // Force collision option to "replace" to accept moving with property loss
                            // Ok since property loss error is raised after checking if the destination already exists
                            await sourceMatch.Select(x => FileNameConflictResolveOptionType.ReplaceExisting).ToListAsync(), progress, cancellationToken);

                        return;
                }
            }
            else if (moveResult.Items.All(x => x.HResult == -1)) // ADS
            {
                // Retry with the StorageFile API
                var failedSources = moveResult.Items.Where(x => !x.Succeeded);
                var moveZip = sourceNoSkip.Zip(destinationNoSkip, (src, dest) => new { src, dest }).Zip(collisionsNoSkip, (z1, coll) => new { z1.src, z1.dest, coll });
                var sourceMatch = await failedSources.Select(x => moveZip.SingleOrDefault(s => s.src.Path.Equals(x.Source, StringComparison.OrdinalIgnoreCase))).Where(x => x is not null).ToListAsync();

                await _fileSystemOperations.MoveItemsAsync(
                    viewModel,
                    await sourceMatch.Select(x => x!.src).ToListAsync(),
                    await sourceMatch.Select(x => x!.dest).ToListAsync(),
                    await sourceMatch.Select(x => x!.coll).ToListAsync(), progress, cancellationToken);

                return;
            }

            return;
        }
    }

    #endregion

    #region dialogs

    private static async Task<bool> RequestAdminOperation(FolderViewViewModel viewModel)
    {
        var dialogService = viewModel.DialogService;
        return await dialogService.ShowDialogAsync(new ElevateConfirmDialogViewModel()) == DialogResult.Primary;
    }

    private static Task<DialogResult> GetFileInUseDialog(FolderViewViewModel viewModel, IEnumerable<string> source, IEnumerable<Win32Process> lockingProcess = null!)
    {
        var titleText = "FileInUseDialog/Title".GetLocalized();
        var subtitleText = lockingProcess.IsEmpty()
            ? "FileInUseDialog/Text".GetLocalized()
            : string.Format("FileInUseByDialog/Text".GetLocalized(), string.Join(", ", lockingProcess.Select(x => $"{x.AppName ?? x.Name} (PID: {x.Pid})")));

        return GetFileListDialog(viewModel, source, titleText, subtitleText, "Retry".GetLocalized(), "Cancel".GetLocalized());
    }

    private static async Task<DialogResult> GetFileListDialog(FolderViewViewModel viewModel, IEnumerable<string> source, string titleText, string descriptionText, string primaryButtonText, string secondaryButtonText)
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

    private static IEnumerable<Win32Process> WhoIsLocking(IEnumerable<string> filesToCheck)
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
