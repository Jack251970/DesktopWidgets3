// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Dialogs;
using Microsoft.UI.Xaml.Controls;
using System.Text;
using Windows.Foundation.Metadata;
using Windows.Storage;

namespace Files.App.Actions;

internal abstract class BaseDecompressArchiveAction : BaseUIAction, IAction
{
	protected readonly IContentPageContext context;
    protected IStorageArchiveService StorageArchiveService { get; } = DependencyExtensions.GetRequiredService<IStorageArchiveService>();

    public abstract string Label { get; }

	public abstract string Description { get; }

	public virtual HotKey HotKey
		=> HotKey.None;

	public override bool IsExecutable =>
		(IsContextPageTypeAdaptedToCommand() &&
        StorageArchiveService.CanDecompress(context.SelectedItems) ||
		CanDecompressInsideArchive()) &&
		FolderViewViewModel.CanShowDialog;

	public BaseDecompressArchiveAction(IFolderViewViewModel folderViewViewModel, IContentPageContext context) : base(folderViewViewModel)
    {
        this.context = context;

        context.PropertyChanged += Context_PropertyChanged;
	}

	public abstract Task ExecuteAsync(object? parameter = null);

	protected bool IsContextPageTypeAdaptedToCommand()
	{
		return
			context.PageType != ContentPageTypes.RecycleBin &&
			context.PageType != ContentPageTypes.ZipFolder &&
			context.PageType != ContentPageTypes.None;
	}

    protected async Task DecompressArchiveHereAsync(bool smart = false)
    {
        if (context.SelectedItems.Count is 0)
        {
            return;
        }

        foreach (var selectedItem in context.SelectedItems)
        {
            var password = string.Empty;
            var archive = await StorageHelpers.ToStorageItem<BaseStorageFile>(selectedItem.ItemPath);
            var currentFolder = await StorageHelpers.ToStorageItem<BaseStorageFolder>(context.ShellPage?.ShellViewModel.CurrentFolder?.ItemPath ?? string.Empty);

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

                if (decompressArchiveViewModel.Password is not null)
                {
                    password = Encoding.UTF8.GetString(decompressArchiveViewModel.Password);
                }
            }

            BaseStorageFolder? destinationFolder = null;

            var isMultipleItems = await FilesystemTasks.Wrap(async () =>
            {
                using var zipFile = await StorageArchiveService.GetSevenZipExtractorAsync(archive.Path);
                if (zipFile is null)
                {
                    return true;
                }

                return zipFile.ArchiveFileData.Select(file =>
                {
                    var pathCharIndex = file.FileName.IndexOfAny(['/', '\\']);
                    if (pathCharIndex == -1)
                    {
                        return file.FileName;
                    }
                    else
                    {
                        return file.FileName[..pathCharIndex];
                    }
                })
                .Distinct().Count() > 1;
            });

            if (smart && currentFolder is not null && isMultipleItems)
            {
                destinationFolder =
                    await FilesystemTasks.Wrap(() =>
                        currentFolder.CreateFolderAsync(
                            SystemIO.Path.GetFileNameWithoutExtension(archive.Path),
                            CreationCollisionOption.GenerateUniqueName).AsTask());
            }
            else
            {
                destinationFolder = currentFolder;
            }

            // Operate decompress
            var result = await FilesystemTasks.Wrap(() =>
                StorageArchiveService.DecompressAsync(FolderViewViewModel, selectedItem.ItemPath, destinationFolder?.Path ?? string.Empty, password));
        }
    }

    protected virtual bool CanDecompressInsideArchive()
	{
		return false;
	}

	protected virtual void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(IContentPageContext.SelectedItems):
				OnPropertyChanged(nameof(IsExecutable));
				break;
		}
	}
}
