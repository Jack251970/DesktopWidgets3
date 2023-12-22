// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.ViewModels.Pages.Widget;
using Files.App.Utils.StatusCenter;
using Files.Core.Data.Enums;
using Windows.Storage;

namespace Files.App.Utils.Storage;

/// <summary>
/// Represents an interface for file system operations.
/// </summary>
/// <remarks>
/// Each operation returns <see cref="Task{IStorageHistory}"/> and the <see cref="IStorageHistory"/> is not saved automatically.
/// </remarks>
public interface IFileSystemOperations : IDisposable
{
    Task<ReturnResult> RenameAsync(
        FolderViewViewModel viewModel,
        IStorageItemWithPath source,
        string newName,
        NameCollisionOption collision,
        IProgress<StatusCenterItemProgressModel> progress,
        bool asAdmin = false);
}