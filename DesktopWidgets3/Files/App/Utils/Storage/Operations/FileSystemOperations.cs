// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Diagnostics;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.ViewModels.Pages.Widget;
using Files.App.Extensions;
using Files.App.Helpers;
using Files.App.Utils.RecycleBin;
using Files.App.Utils.StatusCenter;
using Files.Core.Data.Enums;
using Files.Shared.Extensions;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation.Metadata;
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
                fsResult = await viewModel.FileSystemViewModel.GetFileFromPathAsync(source.Path)
                    .OnSuccess((t) => t.DeleteAsync(permanently ? StorageDeleteOption.PermanentDelete : StorageDeleteOption.Default).AsTask());
            }
            else if (source.ItemType == FilesystemItemType.Directory)
            {
                fsResult = await viewModel.FileSystemViewModel.GetFolderFromPathAsync(source.Path)
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
            // FILESTODO: Retry
            await DialogDisplayHelper.ShowDialogAsync(viewModel, DynamicDialogFactory.GetFor_FileInUseDialog());
        }

        if (deleteFromRecycleBin)
        {
            // Recycle bin also stores a file starting with $I for each item
            var iFilePath = Path.Combine(Path.GetDirectoryName(source.Path)!, Path.GetFileName(source.Path).Replace("$R", "$I", StringComparison.Ordinal));

            await viewModel.FileSystemViewModel.GetFileFromPathAsync(iFilePath)
                .OnSuccess(iFile => iFile.DeleteAsync(StorageDeleteOption.PermanentDelete).AsTask());
        }
        fsProgress.ReportStatus(fsResult);

        if (fsResult)
        {
            await viewModel.FileSystemViewModel.RemoveFileOrFolderAsync(source.Path);

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

    #region create shortcuts

    public Task CreateShortcutItemsAsync(FolderViewViewModel viewModel, IList<IStorageItemWithPath> source, IList<string> destination, IProgress<StatusCenterItemProgressModel> progress, CancellationToken token)
    {
        // TODO: Allow creating shortcuts
        throw new NotImplementedException("Cannot create shortcuts in UWP.");
    }

    #endregion

    #region copy items

    public Task CopyAsync(FolderViewViewModel viewModel, IStorageItem source, string destination, NameCollisionOption collision, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken)
    {
        return CopyAsync(viewModel, source.FromStorageItem(), destination, collision, progress, cancellationToken);
    }

    public async Task CopyAsync(FolderViewViewModel viewModel, IStorageItemWithPath source, string destination, NameCollisionOption collision, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken)
    {
        StatusCenterItemProgressModel fsProgress = new(
            progress,
            true,
            FileSystemStatusCode.InProgress);

        fsProgress.Report();

        if (destination.StartsWith(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.Ordinal))
        {
            fsProgress.ReportStatus(FileSystemStatusCode.Unauthorized);

            // Do not paste files and folders inside the recycle bin
            await DialogDisplayHelper.ShowDialogAsync(
                viewModel,
                "ErrorDialogThisActionCannotBeDone".GetLocalized(),
                "ErrorDialogUnsupportedOperation".GetLocalized());

            return;
        }

        IStorageItem copiedItem = null!;

        if (source.ItemType == FilesystemItemType.Directory)
        {
            if (!string.IsNullOrWhiteSpace(source.Path) &&
                PathNormalization.GetParentDir(destination).IsSubPathOf(source.Path)) // We check if user tried to copy anything above the source.ItemPath
            {
                var destinationName = destination.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).Last();
                var sourceName = source.Path.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).Last();

                ContentDialog dialog = new()
                {
                    Title = "ErrorDialogThisActionCannotBeDone".GetLocalized(),
                    Content = $"{"ErrorDialogTheDestinationFolder".GetLocalized()} ({destinationName}) {"ErrorDialogIsASubfolder".GetLocalized()} ({sourceName})",
                    //PrimaryButtonText = "Skip".GetLocalizedResource(),
                    CloseButtonText = "Cancel".GetLocalized()
                };

                if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
                {
                    dialog.XamlRoot = viewModel.WidgetWindow.Content.XamlRoot;
                }

                var result = await dialog.TryShowAsync(viewModel);

                if (result == ContentDialogResult.Primary)
                {
                    fsProgress.ReportStatus(FileSystemStatusCode.InProgress | FileSystemStatusCode.Success);
                }
                else
                {
                    fsProgress.ReportStatus(FileSystemStatusCode.InProgress | FileSystemStatusCode.Generic);
                }

                return;
            }
            else
            {
                // CopyFileFromApp only works on file not directories
                var fsSourceFolder = await source.ToStorageItemResult();
                var fsDestinationFolder = await viewModel.FileSystemViewModel.GetFolderFromPathAsync(PathNormalization.GetParentDir(destination));
                var fsResult = (FilesystemResult)(fsSourceFolder.ErrorCode | fsDestinationFolder.ErrorCode);

                if (fsResult)
                {
                    if (fsSourceFolder.Result is IPasswordProtectedItem ppis)
                    {
                        ppis.ViewModel = viewModel;
                        ppis.PasswordRequestedCallback = UIFileSystemHelpers.RequestPassword;
                    }

                    var fsCopyResult = await FilesystemTasks.Wrap(() => CloneDirectoryAsync((BaseStorageFolder)fsSourceFolder, (BaseStorageFolder)fsDestinationFolder, fsSourceFolder.Result.Name, collision.Convert()));

                    if (fsSourceFolder.Result is IPasswordProtectedItem ppiu)
                    {
                        ppiu.ViewModel = viewModel;
                        ppiu.PasswordRequestedCallback = null!;
                    }

                    if (fsCopyResult == FileSystemStatusCode.AlreadyExists)
                    {
                        fsProgress.ReportStatus(FileSystemStatusCode.AlreadyExists);

                        return;
                    }

                    if (fsCopyResult)
                    {
                        if (NativeFileOperationsHelper.HasFileAttribute(source.Path, System.IO.FileAttributes.Hidden))
                        {
                            // The source folder was hidden, apply hidden attribute to destination
                            NativeFileOperationsHelper.SetFileAttribute(fsCopyResult.Result.Path, System.IO.FileAttributes.Hidden);
                        }

                        copiedItem = (BaseStorageFolder)fsCopyResult;
                    }

                    fsResult = fsCopyResult;
                }

                if (fsResult == FileSystemStatusCode.Unauthorized)
                {
                    // Cannot do anything, already tried with admin FTP
                }

                fsProgress.ReportStatus(fsResult.ErrorCode);

                if (!fsResult)
                {
                    return;
                }
            }
        }
        else if (source.ItemType == FilesystemItemType.File)
        {
            var fsResult = (FilesystemResult)await Task.Run(() => NativeFileOperationsHelper.CopyFileFromApp(source.Path, destination, true));

            if (!fsResult)
            {
                var destinationResult = await viewModel.FileSystemViewModel.GetFolderFromPathAsync(PathNormalization.GetParentDir(destination));
                var sourceResult = await source.ToStorageItemResult();
                fsResult = sourceResult.ErrorCode | destinationResult.ErrorCode;

                if (fsResult)
                {
                    if (sourceResult.Result is IPasswordProtectedItem ppis)
                    {
                        ppis.ViewModel = viewModel;
                        ppis.PasswordRequestedCallback = UIFileSystemHelpers.RequestPassword;
                    }

                    var file = (BaseStorageFile)sourceResult;
                    var fsResultCopy = new FilesystemResult<BaseStorageFile>(null!, FileSystemStatusCode.Generic);
                    if (string.IsNullOrEmpty(file.Path) && collision == NameCollisionOption.GenerateUniqueName)
                    {
                        // If collision is GenerateUniqueName we will manually check for existing file and generate a new name
                        // HACK: If file is dragged from zip file in windows explorer for example. The file path is empty and
                        // GenerateUniqueName isn't working correctly. Below is a possible solution.
                        var desiredNewName = Path.GetFileName(file.Name);
                        var nameWithoutExt = Path.GetFileNameWithoutExtension(desiredNewName);
                        var extension = Path.GetExtension(desiredNewName);
                        ushort attempt = 1;

                        do
                        {
                            fsResultCopy = await FilesystemTasks.Wrap(() => file.CopyAsync(destinationResult.Result, desiredNewName, NameCollisionOption.FailIfExists).AsTask());
                            desiredNewName = $"{nameWithoutExt} ({attempt}){extension}";
                        } while (fsResultCopy.ErrorCode == FileSystemStatusCode.AlreadyExists && ++attempt < 1024);
                    }
                    else
                    {
                        fsResultCopy = await FilesystemTasks.Wrap(() => file.CopyAsync(destinationResult.Result, Path.GetFileName(file.Name), collision).AsTask());
                    }

                    if (sourceResult.Result is IPasswordProtectedItem ppiu)
                    {
                        ppiu.ViewModel = viewModel;
                        ppiu.PasswordRequestedCallback = null!;
                    }

                    if (fsResultCopy == FileSystemStatusCode.AlreadyExists)
                    {
                        fsProgress.ReportStatus(FileSystemStatusCode.AlreadyExists);

                        return;
                    }

                    if (fsResultCopy)
                    {
                        copiedItem = fsResultCopy.Result;
                    }

                    fsResult = fsResultCopy;
                }

                if (fsResult == FileSystemStatusCode.Unauthorized)
                {
                    // Cannot do anything, already tried with admin FTP
                }
            }

            fsProgress.ReportStatus(fsResult.ErrorCode);

            if (!fsResult)
            {
                return;
            }
        }

        if (collision == NameCollisionOption.ReplaceExisting)
        {
            fsProgress.ReportStatus(FileSystemStatusCode.Success);

            // Cannot undo overwrite operation
            return;
        }
    }

    public async Task CopyItemsAsync(FolderViewViewModel viewModel, IList<IStorageItem> source, IList<string> destination, IList<FileNameConflictResolveOptionType> collisions, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken)
    {
        await CopyItemsAsync(viewModel, await source.Select((item) => item.FromStorageItem()).ToListAsync(), destination, collisions, progress, cancellationToken);
    }

    public async Task CopyItemsAsync(FolderViewViewModel viewModel, IList<IStorageItemWithPath> source, IList<string> destination, IList<FileNameConflictResolveOptionType> collisions, IProgress<StatusCenterItemProgressModel> progress, CancellationToken token, bool asAdmin = false)
    {
        StatusCenterItemProgressModel fsProgress = new(progress, true, FileSystemStatusCode.InProgress, source.Count);
        fsProgress.Report();

        for (var i = 0; i < source.Count; i++)
        {
            if (token.IsCancellationRequested)
            {
                break;
            }

            if (collisions[i] != FileNameConflictResolveOptionType.Skip)
            {
                await CopyAsync(viewModel, source[i], destination[i], collisions[i].Convert(), null!, token);
            }

            fsProgress.AddProcessedItemsCount(1);
            fsProgress.Report();

        }
    }

    #endregion

    #region move items

    public Task MoveAsync(FolderViewViewModel viewModel, IStorageItem source, string destination, NameCollisionOption collision, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken)
    {
        return MoveAsync(viewModel, source.FromStorageItem(), destination, collision, progress, cancellationToken);
    }

    public async Task MoveAsync(FolderViewViewModel viewModel, IStorageItemWithPath source, string destination, NameCollisionOption collision, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken)
    {
        StatusCenterItemProgressModel fsProgress = new(
            progress,
            true,
            FileSystemStatusCode.InProgress);

        fsProgress.Report();

        if (source.Path == destination)
        {
            fsProgress.ReportStatus(FileSystemStatusCode.Success);

            return;
        }

        if (string.IsNullOrWhiteSpace(source.Path))
        {
            // Cannot move (only copy) files from MTP devices because:
            // StorageItems returned in DataPackageView are read-only
            // The item.Path property will be empty and there's no way of retrieving a new StorageItem with R/W access
            await CopyAsync(viewModel, source, destination, collision, progress, cancellationToken);

            return;
        }

        if (destination.StartsWith(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.Ordinal))
        {
            fsProgress.ReportStatus(FileSystemStatusCode.Unauthorized);

            // Do not paste files and folders inside the recycle bin
            await DialogDisplayHelper.ShowDialogAsync(
                viewModel,
                "ErrorDialogThisActionCannotBeDone".GetLocalized(),
                "ErrorDialogUnsupportedOperation".GetLocalized());

            return;
        }

        IStorageItem movedItem = null!;

        if (source.ItemType == FilesystemItemType.Directory)
        {
            // Also check if user tried to move anything above the source.ItemPath
            if (!string.IsNullOrWhiteSpace(source.Path) &&
                PathNormalization.GetParentDir(destination).IsSubPathOf(source.Path))
            {
                var destinationName = destination.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).Last();
                var sourceName = source.Path.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).Last();

                ContentDialog dialog = new()
                {
                    Title = "ErrorDialogThisActionCannotBeDone".GetLocalized(),
                    Content = $"{"ErrorDialogTheDestinationFolder".GetLocalized()} ({destinationName}) {"ErrorDialogIsASubfolder".GetLocalized()} ({sourceName})",
                    //PrimaryButtonText = "Skip".GetLocalizedResource(),
                    CloseButtonText = "Cancel".GetLocalized()
                };

                if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
                {
                    dialog.XamlRoot = viewModel.WidgetWindow.Content.XamlRoot;
                }

                var result = await dialog.TryShowAsync(viewModel);

                if (result == ContentDialogResult.Primary)
                {
                    fsProgress.ReportStatus(FileSystemStatusCode.InProgress | FileSystemStatusCode.Success);
                }
                else
                {
                    fsProgress.ReportStatus(FileSystemStatusCode.InProgress | FileSystemStatusCode.Generic);
                }

                return;
            }
            else
            {
                var fsResult = (FilesystemResult)await Task.Run(() => NativeFileOperationsHelper.MoveFileFromApp(source.Path, destination));

                if (!fsResult)
                {
                    Debug.WriteLine(System.Runtime.InteropServices.Marshal.GetLastWin32Error());

                    var fsSourceFolder = await source.ToStorageItemResult();
                    var fsDestinationFolder = await viewModel.FileSystemViewModel.GetFolderFromPathAsync(PathNormalization.GetParentDir(destination));
                    fsResult = fsSourceFolder.ErrorCode | fsDestinationFolder.ErrorCode;

                    if (fsResult)
                    {
                        if (fsSourceFolder.Result is IPasswordProtectedItem ppis)
                        {
                            ppis.ViewModel = viewModel;
                            ppis.PasswordRequestedCallback = UIFileSystemHelpers.RequestPassword;
                        }  

                        var srcFolder = (BaseStorageFolder)fsSourceFolder;
                        var fsResultMove = await FilesystemTasks.Wrap(() => srcFolder.MoveAsync(fsDestinationFolder.Result, collision).AsTask());

                        if (!fsResultMove) // Use generic move folder operation (move folder items one by one)
                        {
                            // Moving folders using Storage API can result in data loss, copy instead
                            //var fsResultMove = await FilesystemTasks.Wrap(() => MoveDirectoryAsync((BaseStorageFolder)fsSourceFolder, (BaseStorageFolder)fsDestinationFolder, fsSourceFolder.Result.Name, collision.Convert(), true));

                            if (await DialogDisplayHelper.ShowDialogAsync(viewModel, "ErrorDialogThisActionCannotBeDone".GetLocalized(), "ErrorDialogUnsupportedMoveOperation".GetLocalized(), "OK".GetLocalized(), "Cancel".GetLocalized()))
                            {
                                fsResultMove = await FilesystemTasks.Wrap(() => CloneDirectoryAsync((BaseStorageFolder)fsSourceFolder, (BaseStorageFolder)fsDestinationFolder, fsSourceFolder.Result.Name, collision.Convert()));
                            }
                        }

                        if (fsSourceFolder.Result is IPasswordProtectedItem ppiu)
                        {
                            ppiu.ViewModel = viewModel;
                            ppiu.PasswordRequestedCallback = null!;
                        }

                        if (fsResultMove == FileSystemStatusCode.AlreadyExists)
                        {
                            fsProgress.ReportStatus(FileSystemStatusCode.AlreadyExists);

                            return;
                        }

                        if (fsResultMove)
                        {
                            if (NativeFileOperationsHelper.HasFileAttribute(source.Path, System.IO.FileAttributes.Hidden))
                            {
                                // The source folder was hidden, apply hidden attribute to destination
                                NativeFileOperationsHelper.SetFileAttribute(fsResultMove.Result.Path, System.IO.FileAttributes.Hidden);
                            }

                            movedItem = (BaseStorageFolder)fsResultMove;
                        }
                        fsResult = fsResultMove;
                    }
                    if (fsResult == FileSystemStatusCode.Unauthorized || fsResult == FileSystemStatusCode.ReadOnly)
                    {
                        // Cannot do anything, already tried with admin FTP
                    }
                }

                fsProgress.ReportStatus(fsResult.ErrorCode);
            }
        }
        else if (source.ItemType == FilesystemItemType.File)
        {
            var fsResult = (FilesystemResult)await Task.Run(() => NativeFileOperationsHelper.MoveFileFromApp(source.Path, destination));

            if (!fsResult)
            {
                var destinationResult = await viewModel.FileSystemViewModel.GetFolderFromPathAsync(PathNormalization.GetParentDir(destination));
                var sourceResult = await source.ToStorageItemResult();
                fsResult = sourceResult.ErrorCode | destinationResult.ErrorCode;

                if (fsResult)
                {
                    if (sourceResult.Result is IPasswordProtectedItem ppis)
                    {
                        ppis.ViewModel = viewModel;
                        ppis.PasswordRequestedCallback = UIFileSystemHelpers.RequestPassword;
                    }

                    var file = (BaseStorageFile)sourceResult;
                    var fsResultMove = await FilesystemTasks.Wrap(() => file.MoveAsync(destinationResult.Result, Path.GetFileName(file.Name), collision).AsTask());

                    if (sourceResult.Result is IPasswordProtectedItem ppiu)
                    {
                        ppiu.ViewModel = viewModel;
                        ppiu.PasswordRequestedCallback = null!;
                    }

                    if (fsResultMove == FileSystemStatusCode.AlreadyExists)
                    {
                        fsProgress.ReportStatus(FileSystemStatusCode.AlreadyExists);

                        return;
                    }

                    if (fsResultMove)
                    {
                        movedItem = file;
                    }

                    fsResult = fsResultMove;
                }
                if (fsResult == FileSystemStatusCode.Unauthorized || fsResult == FileSystemStatusCode.ReadOnly)
                {
                    // Cannot do anything, already tried with admin FTP
                }
            }
            fsProgress.ReportStatus(fsResult.ErrorCode);
        }

        if (collision == NameCollisionOption.ReplaceExisting)
        {
            // Cannot undo overwrite operation
            return;
        }

        var sourceInCurrentFolder = PathNormalization.TrimPath(viewModel.FileSystemViewModel.CurrentFolder!.ItemPath) ==
            PathNormalization.GetParentDir(source.Path);
        if (fsProgress.Status == FileSystemStatusCode.Success && sourceInCurrentFolder)
        {
            await viewModel.FileSystemViewModel.RemoveFileOrFolderAsync(source.Path);
            await viewModel.FileSystemViewModel.ApplyFilesAndFoldersChangesAsync();
        }
    }

    public async Task MoveItemsAsync(FolderViewViewModel viewModel, IList<IStorageItem> source, IList<string> destination, IList<FileNameConflictResolveOptionType> collisions, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken)
    {
        await MoveItemsAsync(viewModel, await source.Select((item) => item.FromStorageItem()).ToListAsync(), destination, collisions, progress, cancellationToken);
    }

    public async Task MoveItemsAsync(FolderViewViewModel viewModel, IList<IStorageItemWithPath> source, IList<string> destination, IList<FileNameConflictResolveOptionType> collisions, IProgress<StatusCenterItemProgressModel> progress, CancellationToken token, bool asAdmin = false)
    {
        StatusCenterItemProgressModel fsProgress = new(progress, true, FileSystemStatusCode.InProgress, source.Count);
        fsProgress.Report();

        for (var i = 0; i < source.Count; i++)
        {
            if (token.IsCancellationRequested)
            {
                break;
            }

            if (collisions[i] != FileNameConflictResolveOptionType.Skip)
            {
                await MoveAsync(viewModel, source[i], destination[i], collisions[i].Convert(), null!, token);
            }

            fsProgress.AddProcessedItemsCount(1);
            fsProgress.Report();
        }
    }

    #endregion

    #region static methods

    private static async Task<BaseStorageFolder> CloneDirectoryAsync(BaseStorageFolder sourceFolder, BaseStorageFolder destinationFolder, string sourceRootName, CreationCollisionOption collision = CreationCollisionOption.FailIfExists)
    {
        var createdRoot = await destinationFolder.CreateFolderAsync(sourceRootName, collision);
        destinationFolder = createdRoot;

        foreach (var fileInSourceDir in await sourceFolder.GetFilesAsync())
        {
            await fileInSourceDir.CopyAsync(destinationFolder, fileInSourceDir.Name, NameCollisionOption.GenerateUniqueName);
        }

        foreach (var folderinSourceDir in await sourceFolder.GetFoldersAsync())
        {
            await CloneDirectoryAsync(folderinSourceDir, destinationFolder, folderinSourceDir.Name);
        }

        return createdRoot;
    }

    #endregion

    public void Dispose()
    {
        
    }
}