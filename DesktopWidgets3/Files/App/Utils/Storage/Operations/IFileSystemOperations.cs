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
public interface IFileSystemOperations : IDisposable
{
    #region create items

    /// <summary>
    /// Creates an item from <paramref name="source"/>
    /// </summary>
    /// <param name="source">FullPath to the item</param>
    /// <param name="process">Progress of the operation</param>
    /// <param name="cancellationToken">Can be cancelled with <see cref="CancellationToken"/></param>
    Task<IStorageItem> CreateAsync(
        FolderViewViewModel viewModel,
        IStorageItemWithPath source,
        IProgress<StatusCenterItemProgressModel> process,
        CancellationToken cancellationToken,
        bool asAdmin = false);

    #endregion

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

    #region copy items

    /// <summary>
    /// Copies <paramref name="source"/> to <paramref name="destination"/> fullPath
    /// </summary>
    /// <param name="source">The source item to be copied</param>
    /// <param name="destination">The destination fullPath</param>
    /// <param name="collision">The item naming collision</param>
    /// <param name="progress">Progress of the operation</param>
    /// <param name="cancellationToken">Can be cancelled with <see cref="CancellationToken"/></param>
    /// <br/>
    /// Source: The <paramref name="source"/> item fullPath (as <see cref="PathWithType"/>)
    /// <br/>
    /// Destination: The <paramref name="destination"/> item fullPath (as <see cref="PathWithType"/>) the <paramref name="source"/> was copied
    /// </returns>
    Task CopyAsync(
        FolderViewViewModel viewModel,
        IStorageItem source,
        string destination,
        NameCollisionOption collision,
        IProgress<StatusCenterItemProgressModel> progress,
        CancellationToken cancellationToken);

    /// <summary>
    /// Copies <paramref name="source"/> to <paramref name="destination"/> fullPath
    /// </summary>
    /// <param name="source">The source item to be copied</param>
    /// <param name="destination">The destination fullPath</param>
    /// <param name="collision">The item naming collision</param>
    /// <param name="progress">Progress of the operation</param>
    /// <param name="cancellationToken">Can be cancelled with <see cref="CancellationToken"/></param>
    /// <br/>
    /// Source: The <paramref name="source"/> item fullPath (as <see cref="PathWithType"/>)
    /// <br/>
    /// Destination: The <paramref name="destination"/> item fullPath (as <see cref="PathWithType"/>) the <paramref name="source"/> was copied
    /// </returns>
    Task CopyAsync(
        FolderViewViewModel viewModel,
        IStorageItemWithPath source,
        string destination,
        NameCollisionOption collision,
        IProgress<StatusCenterItemProgressModel> progress,
        CancellationToken cancellationToken);

    /// <summary>
    /// Copies <paramref name="source"/> to <paramref name="destination"/> fullPath
    /// </summary>
    Task CopyItemsAsync(
        FolderViewViewModel viewModel,
        IList<IStorageItem> source,
        IList<string> destination,
        IList<FileNameConflictResolveOptionType> collisions,
        IProgress<StatusCenterItemProgressModel> progress,
        CancellationToken cancellationToken);

    /// <summary>
    /// Copies <paramref name="source"/> to <paramref name="destination"/> fullPath
    /// </summary>
    Task CopyItemsAsync(
        FolderViewViewModel viewModel,
        IList<IStorageItemWithPath> source,
        IList<string> destination,
        IList<FileNameConflictResolveOptionType> collisions,
        IProgress<StatusCenterItemProgressModel> progress,
        CancellationToken cancellationToken,
        bool asAdmin = false);

    #endregion

    #region move items

    /// <summary>
    /// Moves <paramref name="source"/> to <paramref name="destination"/> fullPath
    /// </summary>
    /// <param name="source">The source item to be moved</param>
    /// <param name="destination">The destination fullPath</param>
    /// <param name="collision">The item naming collision</param>
    /// <param name="progress">Progress of the operation</param>
    /// <param name="cancellationToken">Can be cancelled with <see cref="CancellationToken"/></param>
    /// <br/>
    /// Source: The source item fullPath (as <see cref="PathWithType"/>)
    /// <br/>
    /// Destination: The <paramref name="destination"/> item fullPath (as <see cref="PathWithType"/>) the <paramref name="source"/> was moved
    /// </returns>
    Task MoveAsync(
        FolderViewViewModel viewModel,
        IStorageItem source,
        string destination,
        NameCollisionOption collision,
        IProgress<StatusCenterItemProgressModel> progress,
        CancellationToken cancellationToken);

    /// <summary>
    /// Moves <paramref name="source"/> to <paramref name="destination"/> fullPath
    /// </summary>
    /// <param name="source">The source item to be moved</param>
    /// <param name="destination">The destination fullPath</param>
    /// <param name="collision">The item naming collision</param>
    /// <param name="progress">Progress of the operation</param>
    /// <param name="cancellationToken">Can be cancelled with <see cref="CancellationToken"/></param>
    /// <br/>
    /// Source: The source item fullPath (as <see cref="PathWithType"/>)
    /// <br/>
    /// Destination: The <paramref name="destination"/> item fullPath (as <see cref="PathWithType"/>) the <paramref name="source"/> was moved
    /// </returns>
    Task MoveAsync(
        FolderViewViewModel viewModel,
        IStorageItemWithPath source,
        string destination,
        NameCollisionOption collision,
        IProgress<StatusCenterItemProgressModel> progress,
        CancellationToken cancellationToken);

    /// <summary>
    /// Moves <paramref name="source"/> to <paramref name="destination"/> fullPath
    /// </summary>
    Task MoveItemsAsync(
        FolderViewViewModel viewModel,
        IList<IStorageItem> source,
        IList<string> destination,
        IList<FileNameConflictResolveOptionType> collisions,
        IProgress<StatusCenterItemProgressModel> progress,
        CancellationToken cancellationToken);

    /// <summary>
    /// Moves <paramref name="source"/> to <paramref name="destination"/> fullPath
    /// </summary>
    Task MoveItemsAsync(
        FolderViewViewModel viewModel,
        IList<IStorageItemWithPath> source,
        IList<string> destination,
        IList<FileNameConflictResolveOptionType> collisions,
        IProgress<StatusCenterItemProgressModel> progress,
        CancellationToken cancellationToken,
        bool asAdmin = false);

    #endregion

    #region create shortcuts

    Task CreateShortcutItemsAsync(
        FolderViewViewModel viewModel,
        IList<IStorageItemWithPath> source,
        IList<string> destination,
        IProgress<StatusCenterItemProgressModel> progress,
        CancellationToken cancellationToken);

    #endregion
}