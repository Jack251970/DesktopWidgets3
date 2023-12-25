// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.Data.Enums;
using Files.Core.Data.Items;
using Windows.Storage;
using Files.App.Utils.StatusCenter;
using DesktopWidgets3.ViewModels.Pages.Widget;

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

    public async Task<ReturnResult> RenameAsync(FolderViewViewModel viewModel, IStorageItemWithPath source, string newName, NameCollisionOption collision, IProgress<StatusCenterItemProgressModel> progress, bool asAdmin = false)
    {
        var mainWindowInstance = viewModel.WidgetWindow;

        if (string.IsNullOrWhiteSpace(source.Path) || source.Path.StartsWith(@"\\?\", StringComparison.Ordinal) || FtpHelpers.IsFtpPath(source.Path) || ZipStorageFolder.IsZipPath(source.Path, false))
        {
            // Fallback to built-in file operations
            return await _fileSystemOperations.RenameAsync(viewModel, source, newName, collision, progress);
        }

        StatusCenterItemProgressModel fsProgress = new(progress, true, FileSystemStatusCode.InProgress);
        fsProgress.Report();

        var renameResult = new ShellOperationResult();
        var (status, response) = await FileOperationsHelpers.RenameItemAsync(source.Path, newName, collision == NameCollisionOption.ReplaceExisting, mainWindowInstance.WindowHandle.ToInt64(), asAdmin);
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
                return ReturnResult.Success;
            }

            // Cannot undo overwrite operation
            return ReturnResult.Failed;
        }
        else
        {
            if (renameResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.Unauthorized))
            {
                if (!asAdmin && await RequestAdminOperation())
                {
                    return await RenameAsync(viewModel, source, newName, collision, progress, true);
                }
            }
            else if (renameResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.InUse))
            {
                var failedSources = renameResult.Items.Where(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.InUse);
                var filePath = failedSources.Select(x => x.HResult == CopyEngineResult.COPYENGINE_E_SHARING_VIOLATION_SRC ? x.Source : x.Destination);
                var lockingProcess = WhoIsLocking(filePath);

                // TODO: Show dialog
                /*switch (await GetFileInUseDialog(filePath, lockingProcess))
                {
                    case DialogResult.Primary:
                        return await RenameAsync(viewModel, source, newName, collision, progress);
                }*/
            }
            else if (renameResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.NameTooLong))
            {
                // Retry with the StorageFile API
                return await _fileSystemOperations.RenameAsync(viewModel, source, newName, collision, progress);
            }
            else if (renameResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.NotFound))
            {
                // TODO: Show error dialog
                //await DialogDisplayHelper.ShowDialogAsync("RenameError/ItemDeleted/Title".GetLocalizedResource(), "RenameError/ItemDeleted/Text".GetLocalizedResource());
            }
            else if (renameResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.AlreadyExists))
            {
                // TODO: Show error dialog
                //await DialogDisplayHelper.ShowDialogAsync("ItemAlreadyExistsDialogTitle".GetLocalizedResource(), "ItemAlreadyExistsDialogContent".GetLocalizedResource());
            }
            // ADS
            else if (renameResult.Items.All(x => x.HResult == -1))
            {
                // Retry with StorageFile API
                return await _fileSystemOperations.RenameAsync(viewModel, source, newName, collision, progress);
            }

            fsProgress.ReportStatus(CopyEngineResult.Convert(renameResult.Items.FirstOrDefault(x => !x.Succeeded)?.HResult));

            return ReturnResult.Failed;
        }
    }

    private async Task<bool> RequestAdminOperation()
    {
        // TODO: Show dialog
        //return await dialogService.ShowDialogAsync(new ElevateConfirmDialogViewModel()) == DialogResult.Primary;
        return false;
    }

    private IEnumerable<Win32Process> WhoIsLocking(IEnumerable<string> filesToCheck)
    {
        return FileOperationsHelpers.CheckFileInUse(filesToCheck.ToArray())!;
    }

    public void Dispose()
    {
        _fileSystemOperations?.Dispose();
        _fileSystemOperations = null!;
    }
}
