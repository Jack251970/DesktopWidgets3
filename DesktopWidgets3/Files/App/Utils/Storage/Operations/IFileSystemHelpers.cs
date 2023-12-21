// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.ViewModels.Pages.Widget;
using Files.App.Utils.Storage;
using Files.Core.Data.Enums;

namespace Files.App.Utils.Storage;

public interface IFileSystemHelpers : IDisposable
{
    Task<ReturnResult> DeleteItemsAsync(
        FolderViewViewModel viewModel, 
        IEnumerable<IStorageItemWithPath> source, 
        DeleteConfirmationPolicies showDialog, 
        bool permanently);
}
