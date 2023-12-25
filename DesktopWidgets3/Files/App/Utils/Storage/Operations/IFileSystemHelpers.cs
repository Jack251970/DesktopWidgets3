// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.ViewModels.Pages.Widget;
using Files.Core.Data.Enums;
using Windows.Storage;

namespace Files.App.Utils.Storage;

public interface IFileSystemHelpers : IDisposable
{
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
}
