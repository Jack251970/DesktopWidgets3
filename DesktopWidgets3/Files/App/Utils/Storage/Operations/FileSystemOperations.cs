// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.ViewModels.Pages.Widget;
using Files.App.Helpers;
using Files.App.Utils.StatusCenter;
using Files.Core.Data.Enums;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;

namespace Files.App.Utils.Storage;

/// <summary>
/// Provides group of file system operation for given page instance.
/// </summary>
public class FileSystemOperations : IFileSystemOperations
{
    public async Task<ReturnResult> RenameAsync(
        FolderViewViewModel viewModel, 
        IStorageItemWithPath source, 
        string newName, 
        NameCollisionOption collision, 
        IProgress<StatusCenterItemProgressModel> progress, 
        bool asAdmin = false)
    {
        StatusCenterItemProgressModel fsProgress = new(progress, true, FileSystemStatusCode.InProgress);

        fsProgress.Report();

        if (Path.GetFileName(source.Path) == newName && collision == NameCollisionOption.FailIfExists)
        {
            fsProgress.ReportStatus(FileSystemStatusCode.AlreadyExists);
            return ReturnResult.Failed;
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
                return ReturnResult.Success;
            }
            else if (renamed == FileSystemStatusCode.Unauthorized)
            {
                // Try again with MoveFileFromApp
                var destination = Path.Combine(Path.GetDirectoryName(source.Path)!, newName);
                if (NativeFileOperationsHelper.MoveFileFromApp(source.Path, destination))
                {
                    fsProgress.ReportStatus(FileSystemStatusCode.Success);
                    return ReturnResult.Success;
                }
                else
                {
                    // Cannot do anything, already tried with admin FTP
                }
            }
            else if (renamed == FileSystemStatusCode.NotAFile || renamed == FileSystemStatusCode.NotAFolder)
            {
                // TODO: Show dialog
                //await DialogDisplayHelper.ShowDialogAsync("RenameError/NameInvalid/Title".GetLocalizedResource(), "RenameError/NameInvalid/Text".GetLocalizedResource());
            }
            else if (renamed == FileSystemStatusCode.NameTooLong)
            {
                // TODO: Show dialog
                //await DialogDisplayHelper.ShowDialogAsync("RenameError/TooLong/Title".GetLocalizedResource(), "RenameError/TooLong/Text".GetLocalizedResource());
            }
            else if (renamed == FileSystemStatusCode.InUse)
            {
                // TODO: Retry
                // TODO: Show dialog
                //await DialogDisplayHelper.ShowDialogAsync(DynamicDialogFactory.GetFor_FileInUseDialog());
            }
            else if (renamed == FileSystemStatusCode.NotFound)
            {
                // TODO: Show dialog
                //await DialogDisplayHelper.ShowDialogAsync("RenameError/ItemDeleted/Title".GetLocalizedResource(), "RenameError/ItemDeleted/Text".GetLocalizedResource());
            }
            else if (renamed == FileSystemStatusCode.AlreadyExists)
            {
                // TODO: Show dialog
                /*var ItemAlreadyExistsDialog = new ContentDialog()
                {
                    Title = "ItemAlreadyExistsDialogTitle".GetLocalizedResource(),
                    Content = "ItemAlreadyExistsDialogContent".GetLocalizedResource(),
                    PrimaryButtonText = "GenerateNewName".GetLocalizedResource(),
                    SecondaryButtonText = "ItemAlreadyExistsDialogSecondaryButtonText".GetLocalizedResource(),
                    CloseButtonText = "Cancel".GetLocalizedResource()
                };

                ContentDialogResult result = await ItemAlreadyExistsDialog.TryShowAsync();*/

                // TODO: handle dialog here
                var result = ContentDialogResult.Primary;

                if (result == ContentDialogResult.Primary)
                {
                    return await RenameAsync(viewModel, source, newName, NameCollisionOption.GenerateUniqueName, progress);
                }
                else if (result == ContentDialogResult.Secondary)
                {
                    return await RenameAsync(viewModel, source, newName, NameCollisionOption.ReplaceExisting, progress);
                }
            }

            fsProgress.ReportStatus(renamed);
        }

        return ReturnResult.Failed;
    }

    public void Dispose()
    {
        
    }
}