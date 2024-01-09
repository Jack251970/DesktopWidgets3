// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.ViewModels.Pages.Widget;
using DesktopWidgets3.Files.App.Helpers;
using DesktopWidgets3.Files.Core.Data.Enums;
using System.ComponentModel;
using Windows.Storage;

namespace DesktopWidgets3.Files.App.Actions;

internal abstract class BaseDeleteAction : BaseUIAction
{
    public override bool IsExecutable =>
        context.HasSelection &&
        !context.IsRenamingItem &&
        context.CanShowDialog;

    public BaseDeleteAction(FolderViewViewModel viewModel) : base(viewModel)
    {
        context.PropertyChanged += Context_PropertyChanged;
    }

    protected async Task DeleteItemsAsync(bool permanently)
    {
        var items = context.SelectedItems.Select(item =>
                StorageHelpers.FromPathAndType(
                    item.ItemPath,
                    item.PrimaryItemAttribute is StorageItemTypes.File
                        ? FilesystemItemType.File
                        : FilesystemItemType.Directory));

        await context.FileSystemHelpers.DeleteItemsAsync(context, items, context.GetSettings().DeleteConfirmationPolicy, permanently);

        await context.FileSystemViewModel.ApplyFilesAndFoldersChangesAsync();
    }

    private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(context.HasSelection))
        {
            OnPropertyChanged(nameof(IsExecutable));
        }
    }
}