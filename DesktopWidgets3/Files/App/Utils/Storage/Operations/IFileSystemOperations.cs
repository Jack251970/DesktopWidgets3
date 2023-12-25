// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.ViewModels.Pages.Widget;
using Files.App.Utils.StatusCenter;
using Windows.Storage;

namespace Files.App.Utils.Storage;

/// <summary>
/// Represents an interface for file system operations.
/// </summary>
public interface IFileSystemOperations : IDisposable
{
    #region delete items

    /// <summary>
    /// Deletes <paramref name="source"/>
    /// </summary>
    /// <param name="source">The source to delete</param>
    /// <param name="progress">Progress of the operation</param>
    /// <param name="permanently">Determines whether <paramref name="source"/> is be deleted permanently</param>
    /// <param name="cancellationToken">Can be cancelled with <see cref="CancellationToken"/></param>
    /// <br/>
    /// Source: The deleted item fullPath (as <see cref="PathWithType"/>)
    /// <br/>
    /// Destination:
    /// <br/>
    /// Returns null if <paramref name="permanently"/> was true
    /// <br/>
    /// If <paramref name="permanently"/> was false, returns path to recycled item
    /// </returns>
    Task DeleteAsync(
        FolderViewViewModel viewModel,
        IStorageItem source,
        IProgress<StatusCenterItemProgressModel> progress,
        bool permanently,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes <paramref name="source"/>
    /// </summary>
    /// <param name="source">The source to delete</param>
    /// <param name="progress">Progress of the operation</param>
    /// <param name="permanently">Determines whether <paramref name="source"/> is be deleted permanently</param>
    /// <param name="cancellationToken">Can be cancelled with <see cref="CancellationToken"/></param>
    /// <br/>
    /// Source: The deleted item fullPath (as <see cref="PathWithType"/>)
    /// <br/>
    /// Destination:
    /// <br/>
    /// Returns null if <paramref name="permanently"/> was true
    /// <br/>
    /// If <paramref name="permanently"/> was false, returns path to recycled item
    /// </returns>
    Task DeleteAsync(
        FolderViewViewModel viewModel,
        IStorageItemWithPath source,
        IProgress<StatusCenterItemProgressModel> progress,
        bool permanently,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes provided <paramref name="source"/>
    /// </summary>
    Task DeleteItemsAsync(
        FolderViewViewModel viewModel,
        IList<IStorageItem> source,
        IProgress<StatusCenterItemProgressModel> progress,
        bool permanently,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes provided <paramref name="source"/>
    /// </summary>
    Task DeleteItemsAsync(
        FolderViewViewModel viewModel,
        IList<IStorageItemWithPath> source,
        IProgress<StatusCenterItemProgressModel> progress,
        bool permanently,
        CancellationToken cancellationToken,
        bool asAdmin = false);

    #endregion

    #region rename items

    /// <summary>
    /// Renames <paramref name="source"/> with <paramref name="newName"/>
    /// </summary>
    /// <param name="source">The item to rename</param>
    /// <param name="newName">Desired new name</param>
    /// <param name="collision">Determines what to do if item already exists</param>
    /// <param name="cancellationToken?">Can be cancelled with <see cref="CancellationToken"/></param>
    /// <br/>
    /// Source: The original item fullPath (as <see cref="PathWithType"/>)
    /// <br/>
    /// Destination: The renamed item fullPath (as <see cref="PathWithType"/>)
    /// </returns>
    Task RenameAsync(
        FolderViewViewModel viewModel,
        IStorageItemWithPath source,
        string newName,
        NameCollisionOption collision,
        IProgress<StatusCenterItemProgressModel> progress,
        CancellationToken cancellationToken,
        bool asAdmin = false);

    #endregion
}