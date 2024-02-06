// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Dialogs;
using Files.App.ViewModels.Dialogs;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using SevenZip;
using System.IO;
using System.Text;
using Windows.Foundation.Metadata;
using Windows.Storage;

namespace Files.App.Utils.Archives;

#pragma warning disable CA2254 // Template should be a static expression

public static class DecompressHelper
{
	/*private static readonly StatusCenterViewModel _statusCenterViewModel = DependencyExtensions.GetService<StatusCenterViewModel>();*/

	private static readonly IThreadingService _threadingService = DependencyExtensions.GetService<IThreadingService>();

	public static async Task<bool> DecompressArchiveAsync(BaseStorageFile archive, BaseStorageFolder destinationFolder, string password, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken)
	{
		using var zipFile = await GetZipFile(archive, password);
		if (zipFile is null)
        {
            return false;
        }

        // Check if the decompress operation canceled
        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        // Fill files

        var buffer = new byte[4096];
		var entriesAmount = zipFile.ArchiveFileData.Where(x => !x.IsDirectory).Count();

        StatusCenterItemProgressModel fsProgress = new(
            progress,
            enumerationCompleted: true,
            FileSystemStatusCode.InProgress,
            entriesAmount)
        {
            TotalSize = zipFile.ArchiveFileData.Select(x => (long)x.Size).Sum()
        };
        fsProgress.Report();

		zipFile.Extracting += (s, e) =>
		{
			if (fsProgress.TotalSize > 0)
            {
                fsProgress.Report(e.BytesProcessed / (double)fsProgress.TotalSize * 100);
            }
        };
		zipFile.FileExtractionStarted += (s, e) =>
		{
			if (cancellationToken.IsCancellationRequested)
            {
                e.Cancel = true;
            }

            if (!e.FileInfo.IsDirectory)
			{
				_threadingService.ExecuteOnUiThreadAsync(() =>
				{
					fsProgress.FileName = e.FileInfo.FileName;
					fsProgress.Report();
				});
			}
		};
		zipFile.FileExtractionFinished += (s, e) =>
		{
			if (!e.FileInfo.IsDirectory)
			{
				fsProgress.AddProcessedItemsCount(1);
				fsProgress.Report();
			}
		};

		try
		{
			// FILESTODO: Get this method return result
			await zipFile.ExtractArchiveAsync(destinationFolder.Path);
		}
		catch (Exception ex)
		{
			App.Logger?.LogWarning(ex, $"Error extracting file: {archive.Name}");

			return false;
		}

		return true;
	}

	private static async Task DecompressArchiveAsync(IFolderViewViewModel folderViewViewModel, BaseStorageFile archive, BaseStorageFolder? destinationFolder, string password)
	{
		if (archive is null || destinationFolder is null)
        {
            return;
        }

        // Initialize a new in-progress status card
        var statusCard = StatusCenterHelper.AddCard_Decompress(
            folderViewViewModel,
			archive.Path.CreateEnumerable(),
			destinationFolder.Path.CreateEnumerable(),
			ReturnResult.InProgress);

		// Operate decompress
		var result = await FilesystemTasks.Wrap(() =>
			DecompressArchiveAsync(archive, destinationFolder, password, statusCard.ProgressEventSource, statusCard.CancellationToken));

		// Remove the in-progress status card
        var _statusCenterViewModel = folderViewViewModel.GetService<StatusCenterViewModel>();
		_statusCenterViewModel.RemoveItem(statusCard);

		if (result.Result)
		{
			// Successful
			StatusCenterHelper.AddCard_Decompress(
                folderViewViewModel,
				archive.Path.CreateEnumerable(),
				destinationFolder.Path.CreateEnumerable(),
				ReturnResult.Success);
		}
		else
		{
			// Error
			StatusCenterHelper.AddCard_Decompress(
                folderViewViewModel,
				archive.Path.CreateEnumerable(),
				destinationFolder.Path.CreateEnumerable(),
				statusCard.CancellationToken.IsCancellationRequested
					? ReturnResult.Cancelled
					: ReturnResult.Failed);
		}
	}

	public static async Task DecompressArchiveAsync(IFolderViewViewModel folderViewViewModel, IShellPage associatedInstance)
	{
		if (associatedInstance == null)
        {
            return;
        }

        var archive = await StorageHelpers.ToStorageItem<BaseStorageFile>(associatedInstance.SlimContentPage?.SelectedItems?.Count is null or 0
			? associatedInstance.FilesystemViewModel.WorkingDirectory
			: associatedInstance.SlimContentPage.SelectedItem!.ItemPath);

		if (archive?.Path is null)
        {
            return;
        }

        var isArchiveEncrypted = await FilesystemTasks.Wrap(() => IsArchiveEncrypted(archive));
		var password = string.Empty;

		DecompressArchiveDialog decompressArchiveDialog = new();
		DecompressArchiveDialogViewModel decompressArchiveViewModel = new(folderViewViewModel, archive)
		{
			IsArchiveEncrypted = isArchiveEncrypted,
			ShowPathSelection = true
		};
		decompressArchiveDialog.ViewModel = decompressArchiveViewModel;

		if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
        {
            decompressArchiveDialog.XamlRoot = folderViewViewModel.XamlRoot;
        }

        var option = await decompressArchiveDialog.TryShowAsync(folderViewViewModel);
		if (option != ContentDialogResult.Primary)
        {
            return;
        }

        if (isArchiveEncrypted)
        {
            password = Encoding.UTF8.GetString(decompressArchiveViewModel.Password!);
        }

        // Check if archive still exists
        if (!StorageHelpers.Exists(archive.Path))
        {
            return;
        }

        BaseStorageFolder destinationFolder = decompressArchiveViewModel.DestinationFolder;
		var destinationFolderPath = decompressArchiveViewModel.DestinationFolderPath;

		if (destinationFolder is null)
		{
			var parentFolder = await StorageHelpers.ToStorageItem<BaseStorageFolder>(Path.GetDirectoryName(archive.Path)!);
			destinationFolder = await FilesystemTasks.Wrap(() => parentFolder.CreateFolderAsync(Path.GetFileName(destinationFolderPath), CreationCollisionOption.GenerateUniqueName).AsTask());
		}

		await DecompressArchiveAsync(folderViewViewModel, archive, destinationFolder, password);

		if (decompressArchiveViewModel.OpenDestinationFolderOnCompletion)
        {
            await NavigationHelpers.OpenPath(folderViewViewModel, destinationFolderPath, associatedInstance, FilesystemItemType.Directory);
        }
    }

	public static async Task DecompressArchiveHereAsync(IFolderViewViewModel folderViewViewModel, IShellPage associatedInstance, bool smart = false)
	{
		if (associatedInstance?.SlimContentPage?.SelectedItems == null)
        {
            return;
        }

        foreach (var selectedItem in associatedInstance.SlimContentPage.SelectedItems)
		{
			var password = string.Empty;
			var archive = await StorageHelpers.ToStorageItem<BaseStorageFile>(selectedItem.ItemPath);
			var currentFolder = await StorageHelpers.ToStorageItem<BaseStorageFolder>(associatedInstance.FilesystemViewModel.CurrentFolder!.ItemPath);

			if (archive?.Path is null)
            {
                return;
            }

            if (await FilesystemTasks.Wrap(() => IsArchiveEncrypted(archive)))
			{
				DecompressArchiveDialog decompressArchiveDialog = new();
				DecompressArchiveDialogViewModel decompressArchiveViewModel = new(folderViewViewModel, archive)
				{
					IsArchiveEncrypted = true,
					ShowPathSelection = false
				};

				decompressArchiveDialog.ViewModel = decompressArchiveViewModel;

				if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
                {
                    decompressArchiveDialog.XamlRoot = folderViewViewModel.XamlRoot;
                }

                var option = await decompressArchiveDialog.TryShowAsync(folderViewViewModel);
				if (option != ContentDialogResult.Primary)
                {
                    return;
                }

                password = Encoding.UTF8.GetString(decompressArchiveViewModel.Password!);
			}

			if (smart && currentFolder is not null && await FilesystemTasks.Wrap(() => IsMultipleItems(archive)))
			{
				var destinationFolder = await FilesystemTasks.Wrap(() => currentFolder.CreateFolderAsync(Path.GetFileNameWithoutExtension(archive.Path), CreationCollisionOption.GenerateUniqueName).AsTask());
				await DecompressArchiveAsync(folderViewViewModel, archive, destinationFolder, password);
			}
			else
            {
                await DecompressArchiveAsync(folderViewViewModel, archive, currentFolder, password);
            }
        }
	}

	public static async Task DecompressArchiveToChildFolderAsync(IFolderViewViewModel folderViewViewModel, IShellPage associatedInstance)
	{
		if (associatedInstance?.SlimContentPage?.SelectedItems == null)
        {
            return;
        }

        foreach (var selectedItem in associatedInstance.SlimContentPage.SelectedItems)
		{
			var password = string.Empty;

			var archive = await StorageHelpers.ToStorageItem<BaseStorageFile>(selectedItem.ItemPath);
			var currentFolder = await StorageHelpers.ToStorageItem<BaseStorageFolder>(associatedInstance.FilesystemViewModel.CurrentFolder!.ItemPath);
			BaseStorageFolder destinationFolder = null!;

			if (archive?.Path is null)
            {
                return;
            }

            if (await FilesystemTasks.Wrap(() => IsArchiveEncrypted(archive)))
			{
				DecompressArchiveDialog decompressArchiveDialog = new();
				DecompressArchiveDialogViewModel decompressArchiveViewModel = new(folderViewViewModel, archive)
				{
					IsArchiveEncrypted = true,
					ShowPathSelection = false
				};
				decompressArchiveDialog.ViewModel = decompressArchiveViewModel;

				if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
                {
                    decompressArchiveDialog.XamlRoot = folderViewViewModel.XamlRoot;
                }

                var option = await decompressArchiveDialog.TryShowAsync(folderViewViewModel);
				if (option != ContentDialogResult.Primary)
                {
                    return;
                }

                password = Encoding.UTF8.GetString(decompressArchiveViewModel.Password!);
			}

			if (currentFolder is not null)
            {
                destinationFolder = await FilesystemTasks.Wrap(() => currentFolder.CreateFolderAsync(Path.GetFileNameWithoutExtension(archive.Path), CreationCollisionOption.GenerateUniqueName).AsTask());
            }

            await DecompressArchiveAsync(folderViewViewModel, archive, destinationFolder, password);
		}
	}

	private static async Task<SevenZipExtractor?> GetZipFile(BaseStorageFile archive, string password = "")
	{
		return await FilesystemTasks.Wrap(async () =>
		{
			var arch = new SevenZipExtractor(await archive.OpenStreamForReadAsync(), password);
			return arch?.ArchiveFileData is null ? null : arch; // Force load archive (1665013614u)
		});
	}

	private static async Task<bool> IsArchiveEncrypted(BaseStorageFile archive)
	{
		using var zipFile = await GetZipFile(archive);
		if (zipFile is null)
        {
            return true;
        }

        return zipFile.ArchiveFileData.Any(file => file.Encrypted || file.Method.Contains("Crypto") || file.Method.Contains("AES"));
	}

	private static async Task<bool> IsMultipleItems(BaseStorageFile archive)
	{
		using var zipFile = await GetZipFile(archive);
		if (zipFile is null)
        {
            return true;
        }

        return zipFile.ArchiveFileData.Count > 1;
	}
}

