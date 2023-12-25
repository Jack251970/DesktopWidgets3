// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.Helpers;
using DesktopWidgets3.ViewModels.Pages.Widget;
using Files.App.Helpers;
using Files.App.Utils.RecycleBin;
using Files.App.Utils.StatusCenter;
using Files.Core.Data.Enums;
using Files.Shared.Extensions;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;

namespace Files.App.Utils.Storage;

/// <summary>
/// Provides group of file system operation for given page instance.
/// </summary>
public class FileSystemOperations : IFileSystemOperations
{
    #region delete items

    public Task DeleteAsync(FolderViewViewModel viewModel, IStorageItem source, IProgress<StatusCenterItemProgressModel> progress, bool permanently, CancellationToken cancellationToken)
    {
        return DeleteAsync(viewModel, source.FromStorageItem(), progress, permanently, cancellationToken);
    }

    public async Task DeleteAsync(FolderViewViewModel viewModel, IStorageItemWithPath source, IProgress<StatusCenterItemProgressModel> progress, bool permanently, CancellationToken cancellationToken)
    {
        StatusCenterItemProgressModel fsProgress = new(
            progress,
            true,
            FileSystemStatusCode.InProgress);

        fsProgress.Report();

        var deleteFromRecycleBin = RecycleBinHelpers.IsPathUnderRecycleBin(source.Path);

        FilesystemResult fsResult = FileSystemStatusCode.InProgress;

        if (permanently)
        {
            fsResult = (FilesystemResult)NativeFileOperationsHelper.DeleteFileFromApp(source.Path);
        }

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

        fsProgress.ReportStatus(fsResult);

        if (fsResult == FileSystemStatusCode.Unauthorized)
        {
            // Cannot do anything, already tried with admin FTP
        }
        else if (fsResult == FileSystemStatusCode.InUse)
        {
            // TODO: Retry
            await DialogDisplayHelper.ShowDialogAsync(viewModel, DynamicDialogFactory.GetFor_FileInUseDialog());
        }

        if (deleteFromRecycleBin)
        {
            // Recycle bin also stores a file starting with $I for each item
            var iFilePath = Path.Combine(Path.GetDirectoryName(source.Path)!, Path.GetFileName(source.Path).Replace("$R", "$I", StringComparison.Ordinal));

            await viewModel.ItemViewModel.GetFileFromPathAsync(iFilePath)
                .OnSuccess(iFile => iFile.DeleteAsync(StorageDeleteOption.PermanentDelete).AsTask());
        }
        fsProgress.ReportStatus(fsResult);

        if (fsResult)
        {
            await viewModel.ItemViewModel.RemoveFileOrFolderAsync(source.Path);

            /*if (!permanently)
            {
                // Enumerate Recycle Bin
                IEnumerable<ShellFileItem> nameMatchItems, items = await RecycleBinHelpers.EnumerateRecycleBin();

                // Get name matching files
                if (FileExtensionHelpers.IsShortcutOrUrlFile(source.Path)) // We need to check if it is a shortcut file
                {
                    nameMatchItems = items.Where((item) => item.FilePath == Path.Combine(Path.GetDirectoryName(source.Path)!, Path.GetFileNameWithoutExtension(source.Path)));
                }
                else
                {
                    nameMatchItems = items.Where((item) => item.FilePath == source.Path);
                }

                // Get newest file
                ShellFileItem item = nameMatchItems.OrderBy((item) => item.RecycleDate).FirstOrDefault()!;
            }*/
        }
        else
        {
            // Stop at first error
        }
    }

    public async Task DeleteItemsAsync(FolderViewViewModel viewModel, IList<IStorageItem> source, IProgress<StatusCenterItemProgressModel> progress, bool permanently, CancellationToken cancellationToken)
    {
        await DeleteItemsAsync(viewModel, await source.Select((item) => item.FromStorageItem()).ToListAsync(), progress, permanently, cancellationToken);
    }

    public async Task DeleteItemsAsync(FolderViewViewModel viewModel, IList<IStorageItemWithPath> source, IProgress<StatusCenterItemProgressModel> progress, bool permanently, CancellationToken token, bool asAdmin = false)
    {
        StatusCenterItemProgressModel fsProgress = new(progress, true, FileSystemStatusCode.InProgress, source.Count);
        fsProgress.Report();

        var originalPermanently = permanently;

        for (var i = 0; i < source.Count; i++)
        {
            if (token.IsCancellationRequested)
            {
                break;
            }

            permanently = RecycleBinHelpers.IsPathUnderRecycleBin(source[i].Path) || originalPermanently;

            await DeleteAsync(viewModel, source[i], progress, permanently, token);
            fsProgress.AddProcessedItemsCount(1);
            fsProgress.Report();
        }
    }

    #endregion

    #region rename items

    public async Task RenameAsync(
        FolderViewViewModel viewModel, 
        IStorageItemWithPath source, 
        string newName, 
        NameCollisionOption collision, 
        IProgress<StatusCenterItemProgressModel> progress,
        CancellationToken cancellationToken,
        bool asAdmin = false)
    {
        StatusCenterItemProgressModel fsProgress = new(progress, true, FileSystemStatusCode.InProgress);

        fsProgress.Report();

        if (Path.GetFileName(source.Path) == newName && collision == NameCollisionOption.FailIfExists)
        {
            fsProgress.ReportStatus(FileSystemStatusCode.AlreadyExists);
        }

        if (!string.IsNullOrWhiteSpace(newName) &&
            !FileSystemHelpers.ContainsRestrictedCharacters(newName) &&
            !FileSystemHelpers.ContainsRestrictedFileName(newName))
        {
            var renamed = await source.ToStorageItemResult()
                .OnSuccess(async (t) =>
                {
                    if (t.Name.Equals(newName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        await t.RenameAsync(newName, NameCollisionOption.ReplaceExisting);
                    }
                    else
                    {
                        await t.RenameAsync(newName, collision);
                    }

                    return t;
                });

            if (renamed)
            {
                fsProgress.ReportStatus(FileSystemStatusCode.Success);
            }
            else if (renamed == FileSystemStatusCode.Unauthorized)
            {
                // Try again with MoveFileFromApp
                var destination = Path.Combine(Path.GetDirectoryName(source.Path)!, newName);
                if (NativeFileOperationsHelper.MoveFileFromApp(source.Path, destination))
                {
                    fsProgress.ReportStatus(FileSystemStatusCode.Success);
                }
                else
                {
                    // Cannot do anything, already tried with admin FTP
                }
            }
            else if (renamed == FileSystemStatusCode.NotAFile || renamed == FileSystemStatusCode.NotAFolder)
            {
                await DialogDisplayHelper.ShowDialogAsync(viewModel, "RenameError/NameInvalid/Title".GetLocalized(), "RenameError/NameInvalid/Text".GetLocalized());
            }
            else if (renamed == FileSystemStatusCode.NameTooLong)
            {
                await DialogDisplayHelper.ShowDialogAsync(viewModel, "RenameError/TooLong/Title".GetLocalized(), "RenameError/TooLong/Text".GetLocalized());
            }
            else if (renamed == FileSystemStatusCode.InUse)
            {
                await DialogDisplayHelper.ShowDialogAsync(viewModel, DynamicDialogFactory.GetFor_FileInUseDialog());
            }
            else if (renamed == FileSystemStatusCode.NotFound)
            {
                await DialogDisplayHelper.ShowDialogAsync(viewModel, "RenameError/ItemDeleted/Title".GetLocalized(), "RenameError/ItemDeleted/Text".GetLocalized());
            }
            else if (renamed == FileSystemStatusCode.AlreadyExists)
            {
                var ItemAlreadyExistsDialog = new ContentDialog()
                {
                    Title = "ItemAlreadyExistsDialogTitle".GetLocalized(),
                    Content = "ItemAlreadyExistsDialogContent".GetLocalized(),
                    PrimaryButtonText = "GenerateNewName".GetLocalized(),
                    SecondaryButtonText = "ItemAlreadyExistsDialogSecondaryButtonText".GetLocalized(),
                    CloseButtonText = "Cancel".GetLocalized()
                };

                var result = await ItemAlreadyExistsDialog.TryShowAsync(viewModel);

                if (result == ContentDialogResult.Primary)
                {
                    await RenameAsync(viewModel, source, newName, NameCollisionOption.GenerateUniqueName, progress, cancellationToken);
                }
                else if (result == ContentDialogResult.Secondary)
                {
                    await RenameAsync(viewModel, source, newName, NameCollisionOption.ReplaceExisting, progress, cancellationToken);
                }
            }

            fsProgress.ReportStatus(renamed);
        }
    }

    #endregion

    public void Dispose()
    {
        
    }
}