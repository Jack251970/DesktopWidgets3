// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Core.Data.Enums;
using Files.Core.Data.Items;
using Windows.Storage;

namespace Files.App.Helpers;

public static class UIFilesystemHelpers
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
                library.Folders.Count == 1) // TODO: handle libraries with multiple folders
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

}
