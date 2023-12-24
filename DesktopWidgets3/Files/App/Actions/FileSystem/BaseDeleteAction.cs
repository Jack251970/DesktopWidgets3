// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.ViewModels.Pages.Widget;
using Files.App.Helpers;
using Files.Core.Data.Enums;
using System.ComponentModel;
using Windows.Storage;

namespace Files.App.Actions;

internal abstract class BaseDeleteAction : BaseUIAction
{
    private readonly FolderViewViewModel _viewModel;

    public override bool IsExecutable =>
        _viewModel.HasSelection &&
        !_viewModel.IsRenamingItem; /*&&
        UIHelpers.CanShowDialog;*/

    public BaseDeleteAction(FolderViewViewModel viewModel)
    {
        _viewModel = viewModel;

        viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    protected async Task DeleteItemsAsync(bool permanently)
    {
        var items = _viewModel.SelectedItems.Select(item =>
                StorageHelpers.FromPathAndType(
                    item.ItemPath,
                    item.PrimaryItemAttribute is StorageItemTypes.File
                        ? FilesystemItemType.File
                        : FilesystemItemType.Directory));

        var deleteConfirmationPolicy = _viewModel.GetSettings().DeleteConfirmationPolicy;
        await _viewModel.FileSystemHelpers.DeleteItemsAsync(_viewModel, items, deleteConfirmationPolicy, permanently);

        await _viewModel.ItemViewModel.ApplyFilesAndFoldersChangesAsync();
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(_viewModel.HasSelection))
        {
            OnPropertyChanged(nameof(IsExecutable));
        }
    }
}