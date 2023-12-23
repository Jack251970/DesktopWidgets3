// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.ViewModels.Pages.Widget;
using Files.Core.Data.Enums;
using Windows.Storage;

namespace Files.App.Utils.Storage;

public interface IFileSystemHelpers : IDisposable
{
    Task<ReturnResult> DeleteItemAsync(FolderViewViewModel viewModel, IStorageItemWithPath source, DeleteConfirmationPolicies showDialog, bool permanently);

    Task<ReturnResult> DeleteItemsAsync(FolderViewViewModel viewModel, IEnumerable<IStorageItemWithPath> source, DeleteConfirmationPolicies showDialog, bool permanently);

    Task<ReturnResult> RenameAsync(FolderViewViewModel viewModel, IStorageItemWithPath source, string newName, NameCollisionOption collision, bool showExtensionDialog = true);
}
