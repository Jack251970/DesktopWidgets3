﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Dialogs;
using Microsoft.UI.Xaml.Controls;
using System.Text;
using Windows.Foundation.Metadata;
using Windows.Storage;

namespace Files.App.Actions;

internal sealed class DecompressArchiveToChildFolderAction(IFolderViewViewModel folderViewViewModel, IContentPageContext context) : BaseDecompressArchiveAction(folderViewViewModel, context)
{
	public override string Label
		=> ComputeLabel();

	public override string Description
		=> "DecompressArchiveToChildFolderDescription".GetLocalizedResource();

    public async override Task ExecuteAsync(object? parameter = null)
    {
        if (context.SelectedItems.Count is 0)
        {
            return;
        }

        foreach (var selectedItem in context.SelectedItems)
        {
            var password = string.Empty;

            var archive = await StorageHelpers.ToStorageItem<BaseStorageFile>(selectedItem.ItemPath);
            var currentFolder = await StorageHelpers.ToStorageItem<BaseStorageFolder>(context.ShellPage?.ShellViewModel.CurrentFolder?.ItemPath!);
            BaseStorageFolder destinationFolder = null!;

            if (archive?.Path is null)
            {
                return;
            }

            if (await FilesystemTasks.Wrap(() => StorageArchiveService.IsEncryptedAsync(archive.Path)))
            {
                DecompressArchiveDialog decompressArchiveDialog = new(FolderViewViewModel);
                DecompressArchiveDialogViewModel decompressArchiveViewModel = new(FolderViewViewModel, archive)
                {
                    IsArchiveEncrypted = true,
                    ShowPathSelection = false
                };
                decompressArchiveDialog.ViewModel = decompressArchiveViewModel;

                if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
                {
                    decompressArchiveDialog.XamlRoot = FolderViewViewModel.XamlRoot;
                }

                var option = await decompressArchiveDialog.TryShowAsync(FolderViewViewModel);
                if (option != ContentDialogResult.Primary)
                {
                    return;
                }

                password = Encoding.UTF8.GetString(decompressArchiveViewModel.Password!);
            }

            if (currentFolder is not null)
            {
                destinationFolder = await FilesystemTasks.Wrap(() => currentFolder.CreateFolderAsync(SystemIO.Path.GetFileNameWithoutExtension(archive.Path), CreationCollisionOption.GenerateUniqueName).AsTask());
            }

            // Operate decompress
            var result = await FilesystemTasks.Wrap(() =>
                StorageArchiveService.DecompressAsync(FolderViewViewModel, selectedItem.ItemPath, destinationFolder?.Path ?? string.Empty, password));
        }
    }

    protected override void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(IContentPageContext.SelectedItems):
				{
					if (IsContextPageTypeAdaptedToCommand())
					{
						OnPropertyChanged(nameof(Label));
						OnPropertyChanged(nameof(IsExecutable));
					}

					break;
				}
		}
	}

	private string ComputeLabel()
	{
		if (context.SelectedItems == null || context.SelectedItems.Count == 0)
        {
            return string.Format("BaseLayoutItemContextFlyoutExtractToChildFolder".GetLocalizedResource(), string.Empty);
        }

        return context.SelectedItems.Count > 1
			? string.Format("BaseLayoutItemContextFlyoutExtractToChildFolder".GetLocalizedResource(), "*")
			: string.Format("BaseLayoutItemContextFlyoutExtractToChildFolder".GetLocalizedResource(), SystemIO.Path.GetFileNameWithoutExtension(context.SelectedItems[0].Name));
	}
}
