// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.ViewModels.Pages.Widget;
using DesktopWidgets3.Files.Core.Data.Enums;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace DesktopWidgets3.Files.App.Utils.Storage;

public interface IFileSystemHelpers : IDisposable
{
    #region Create

    /// <summary>
    /// Creates an item from <paramref name="source"/>
    /// </summary>
    /// <param name="viewModel">The <see cref="FolderViewViewModel"/> that contains the <paramref name="source"/></param>
    /// <param name="source">FullPath to the item</param>
    /// <returns><see cref="ReturnResult"/> of performed operation</returns>
    Task<(ReturnResult, IStorageItem?)> CreateAsync(FolderViewViewModel viewModel, IStorageItemWithPath source);

    #endregion

    #region Delete

    /// <summary>
    /// Deletes provided <paramref name="source"/>
    /// </summary>
    /// <param name="viewModel">The <see cref="FolderViewViewModel"/> that contains the <paramref name="source"/></param>
    /// <param name="source">The <paramref name="source"/> to delete</param>
    /// <param name="showDialog">Determines whether to show delete confirmation dialog</param>
    /// <param name="permanently">Determines whether <paramref name="source"/> is be deleted permanently</param>
    /// <returns><see cref="ReturnResult"/> of performed operation</returns>
    Task<ReturnResult> DeleteItemsAsync(FolderViewViewModel viewModel, IEnumerable<IStorageItem> source, DeleteConfirmationPolicies showDialog, bool permanently);

    /// <summary>
    /// Deletes provided <paramref name="source"/>
    /// </summary>
    /// <param name="viewModel">The <see cref="FolderViewViewModel"/> that contains the <paramref name="source"/></param>
    /// <param name="source">The <paramref name="source"/> to delete</param>
    /// <param name="showDialog">Determines whether to show delete confirmation dialog</param>
    /// <param name="permanently">Determines whether <paramref name="source"/> is be deleted permanently</param>
    /// <returns><see cref="ReturnResult"/> of performed operation</returns>
    Task<ReturnResult> DeleteItemAsync(FolderViewViewModel viewModel, IStorageItem source, DeleteConfirmationPolicies showDialog, bool permanently);

    /// <summary>
    /// Deletes provided <paramref name="source"/>
    /// </summary>
    /// <param name="viewModel">The <see cref="FolderViewViewModel"/> that contains the <paramref name="source"/></param>
    /// <param name="source">The <paramref name="source"/> to delete</param>
    /// <param name="showDialog">Determines whether to show delete confirmation dialog</param>
    /// <param name="permanently">Determines whether <paramref name="source"/> is be deleted permanently</param>
    /// <returns><see cref="ReturnResult"/> of performed operation</returns>
    Task<ReturnResult> DeleteItemsAsync(FolderViewViewModel viewModel, IEnumerable<IStorageItemWithPath> source, DeleteConfirmationPolicies showDialog, bool permanently);

    /// <summary>
    /// Deletes provided <paramref name="source"/>
    /// </summary>
    /// <param name="viewModel">The <see cref="FolderViewViewModel"/> that contains the <paramref name="source"/></param>
    /// <param name="source">The <paramref name="source"/> to delete</param>
    /// <param name="showDialog">Determines whether to show delete confirmation dialog</param>
    /// <param name="permanently">Determines whether <paramref name="source"/> is be deleted permanently</param>
    /// <returns><see cref="ReturnResult"/> of performed operation</returns>
    Task<ReturnResult> DeleteItemAsync(FolderViewViewModel viewModel, IStorageItemWithPath source, DeleteConfirmationPolicies showDialog, bool permanently);

    #endregion Delete

    #region Rename

    /// <summary>
    /// Renames <paramref name="source"/> with <paramref name="newName"/>
    /// </summary>
    /// <param name="viewModel">The <see cref="FolderViewViewModel"/> that contains the <paramref name="source"/></param>
    /// <param name="source">The item to rename</param>
    /// <param name="newName">Desired new name</param>
    /// <param name="collision">Determines what to do if item already exists</param>
    /// <param name="showExtensionDialog">Determines wheteher the Extension Modified Dialog is shown</param>
    /// <returns><see cref="ReturnResult"/> of performed operation</returns>
    Task<ReturnResult> RenameAsync(FolderViewViewModel viewModel, IStorageItem source, string newName, NameCollisionOption collision, bool showExtensionDialog = true);

    /// <summary>
    /// Renames <paramref name="source"/> fullPath with <paramref name="newName"/>
    /// </summary>
    /// <param name="viewModel">The <see cref="FolderViewViewModel"/> that contains the <paramref name="source"/></param>
    /// <param name="source">The item to rename</param>
    /// <param name="newName">Desired new name</param>
    /// <param name="collision">Determines what to do if item already exists</param>
    /// <param name="showExtensionDialog">Determines wheteher the Extension Modified Dialog is shown</param>
    /// <returns><see cref="ReturnResult"/> of performed operation</returns>
    Task<ReturnResult> RenameAsync(FolderViewViewModel viewModel, IStorageItemWithPath source, string newName, NameCollisionOption collision, bool showExtensionDialog = true);

    #endregion

    #region Paste

    /// <summary>
    /// Performs relevant operation based on <paramref name="operation"/>
    /// </summary>
    /// <param name="viewModel">The <see cref="FolderViewViewModel"/> that contains the <paramref name="source"/></param></param>
    /// <param name="operation">The operation</param>
    /// <param name="packageView">The package view data</param>
    /// <param name="destination">Destination directory to perform the operation
    /// <param name="showDialog">Determines whether to show dialog</param>
    /// <br/>
    /// <br/>
    /// Note:
    /// <br/>
    /// The <paramref name="destination"/> is NOT fullPath</param>
    /// <returns><see cref="ReturnResult"/> of performed operation</returns>
    Task<ReturnResult> PerformOperationTypeAsync(FolderViewViewModel viewModel, DataPackageOperation operation, DataPackageView packageView, string destination, bool showDialog, bool isDestinationExecutable = false, bool isDestinationPython = false);

    #endregion

    #region Move

    /// <summary>
    /// Moves <paramref name="source"/> to <paramref name="destination"/> fullPath
    /// </summary>
    /// <param name="viewModel">The <see cref="FolderViewViewModel"/> that contains the <paramref name="source"/></param></param>
    /// <param name="source">The source items to be moved</param>
    /// <param name="destination">The destination fullPath</param>
    /// <param name="showDialog">Determines whether to show move dialog</param>
    /// <returns><see cref="ReturnResult"/> of performed operation</returns>
    Task<ReturnResult> MoveItemsAsync(FolderViewViewModel viewModel, IEnumerable<IStorageItem> source, IEnumerable<string> destination, bool showDialog);

    /// <summary>
    /// Moves <paramref name="source"/> to <paramref name="destination"/> fullPath
    /// </summary>
    /// <param name="viewModel">The <see cref="FolderViewViewModel"/> that contains the <paramref name="source"/></param></param>
    /// <param name="source">The source to move</param>
    /// <param name="destination">The destination fullPath</param>
    /// <param name="showDialog">Determines whether to show move dialog</param>
    /// <returns><see cref="ReturnResult"/> of performed operation</returns>
    Task<ReturnResult> MoveItemAsync(FolderViewViewModel viewModel, IStorageItem source, string destination, bool showDialog);

    /// <summary>
    /// Moves <paramref name="source"/> to <paramref name="destination"/> fullPath
    /// </summary>
    /// <param name="viewModel">The <see cref="FolderViewViewModel"/> that contains the <paramref name="source"/></param></param>
    /// <param name="source">The source items to be moved</param>
    /// <param name="destination">The destination fullPath</param>
    /// <param name="showDialog">Determines whether to show move dialog</param>
    /// <returns><see cref="ReturnResult"/> of performed operation</returns>
    Task<ReturnResult> MoveItemsAsync(FolderViewViewModel viewModel, IEnumerable<IStorageItemWithPath> source, IEnumerable<string> destination, bool showDialog);

    /// <summary>
    /// Moves <paramref name="source"/> to <paramref name="destination"/> fullPath
    /// </summary>
    /// <param name="viewModel">The <see cref="FolderViewViewModel"/> that contains the <paramref name="source"/></param></param>
    /// <param name="source">The source to move</param>
    /// <param name="destination">The destination fullPath</param>
    /// <param name="showDialog">Determines whether to show move dialog</param>
    /// <returns><see cref="ReturnResult"/> of performed operation</returns>
    Task<ReturnResult> MoveItemAsync(FolderViewViewModel viewModel, IStorageItemWithPath source, string destination, bool showDialog);

    /// <summary>
    /// Moves items from clipboard to <paramref name="destination"/> fullPath
    /// </summary>
    /// <param name="viewModel">The <see cref="FolderViewViewModel"/> that contains the <paramref name="source"/></param></param>
    /// <param name="packageView">Clipboard data</param>
    /// <param name="destination">Destination directory to perform the operation
    /// <param name="showDialog">Determines whether to show move dialog</param>
    /// <br/>
    /// <br/>
    /// Note:
    /// <br/>
    /// The <paramref name="destination"/> is NOT fullPath</param>
    /// <returns><see cref="ReturnResult"/> of performed operation</returns>
    Task<ReturnResult> MoveItemsFromClipboard(FolderViewViewModel viewModel, DataPackageView packageView, string destination, bool showDialog);

    #endregion
}
