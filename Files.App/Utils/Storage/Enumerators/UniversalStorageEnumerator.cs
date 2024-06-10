// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;
using Windows.Storage;

namespace Files.App.Utils.Storage;

public static class UniversalStorageEnumerator
{
	public static async Task<List<ListedItem>> ListEntries(
        IFolderViewViewModel folderViewViewModel,
        BaseStorageFolder rootFolder,
		StorageFolderWithPath currentStorageFolder,
		int countLimit,
		Func<List<ListedItem>, Task> intermediateAction,
		Dictionary<string, BitmapImage> defaultIconPairs,
        CancellationToken cancellationToken)
	{
		var sampler = new IntervalSampler(500);
		var tempList = new List<ListedItem>();
		uint count = 0;
		var firstRound = true;

		var userSettingsService = folderViewViewModel.GetRequiredService<IUserSettingsService>();

		while (true)
		{
			IReadOnlyList<IStorageItem> items;

			uint maxItemsToRetrieve = 300;

			if (intermediateAction is null)
			{
				// without intermediate action increase batches significantly
				maxItemsToRetrieve = 1000;
			}
			else if (firstRound)
			{
				maxItemsToRetrieve = 32;
				firstRound = false;
			}

			try
			{
				items = await rootFolder.GetItemsAsync(count, maxItemsToRetrieve);
				if (items is null || items.Count == 0)
				{
					break;
				}
			}
			catch (NotImplementedException)
			{
				break;
			}
			catch (Exception ex) when (
				ex is UnauthorizedAccessException ||
				ex is FileNotFoundException ||
				(uint)ex.HResult == 0x80070490) // ERROR_NOT_FOUND
			{
				// If some unexpected exception is thrown - enumerate this folder file by file - just to be sure
				items = await EnumerateFileByFile(rootFolder, count, maxItemsToRetrieve);
			}
			catch (Exception ex)
			{
				App.Logger?.LogWarning(ex, "Error enumerating directory contents.");

				break;
			}

			foreach (var item in items)
			{
				var startWithDot = item.Name.StartsWith('.');
				if (!startWithDot || userSettingsService.FoldersSettingsService.ShowDotFiles)
                {
					if (item.IsOfType(StorageItemTypes.Folder))
					{
						var folder = await AddFolderAsync(folderViewViewModel, item.AsBaseStorageFolder()!, currentStorageFolder, cancellationToken);
						if (folder is not null)
						{
							if (defaultIconPairs?.ContainsKey(string.Empty) ?? false)
                            {
                                folder.FileImage = defaultIconPairs[string.Empty];
                            }

                            tempList.Add(folder);
						}
					}
					else
					{
						var fileEntry = await AddFileAsync(folderViewViewModel, item.AsBaseStorageFile()!, currentStorageFolder, cancellationToken);
						if (fileEntry is not null)
						{
							if (defaultIconPairs is not null)
							{
								if (!string.IsNullOrEmpty(fileEntry.FileExtension))
								{
									var lowercaseExtension = fileEntry.FileExtension.ToLowerInvariant();
		
									if (defaultIconPairs.TryGetValue(lowercaseExtension, out var value))
                                    {
                                        fileEntry.FileImage = value;
                                    }
                                }
							}

							tempList.Add(fileEntry);
						}
					}
				}

				if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }

			count += maxItemsToRetrieve;

			if (countLimit > -1 && count >= countLimit)
            {
                break;
            }

            if (intermediateAction is not null && (items.Count == maxItemsToRetrieve || sampler.CheckNow()))
			{
				await intermediateAction(tempList);

				// clear the temporary list every time we do an intermediate action
				tempList.Clear();
			}
		}

		return tempList;
	}

	private static async Task<IReadOnlyList<IStorageItem>> EnumerateFileByFile(BaseStorageFolder rootFolder, uint startFrom, uint itemsToIterate)
	{
		var tempList = new List<IStorageItem>();

		for (var i = startFrom; i < startFrom + itemsToIterate; i++)
		{
			IStorageItem item;
			try
			{
				var results = await rootFolder.GetItemsAsync(i, 1);

                // CHANGE: Use property instead of Linq Enumerable method FirstOrDefault().
                item = (results?.Count > 0 ? results[0] : default)!;
                if (item == default)
                {
                    break;
                }
            }
			catch (NotImplementedException)
			{
				break;
			}
			catch (Exception ex) when (
				ex is UnauthorizedAccessException ||
				ex is FileNotFoundException ||
				(uint)ex.HResult == 0x80070490) // ERROR_NOT_FOUND
			{
				continue;
			}
			catch (Exception ex)
			{
				App.Logger?.LogWarning(ex, "Error enumerating directory contents.");
				break;
			}

			tempList.Add(item);
		}

		return tempList;
	}

	public static async Task<ListedItem> AddFolderAsync(
        IFolderViewViewModel folderViewViewModel,
		BaseStorageFolder folder,
		StorageFolderWithPath currentStorageFolder,
		CancellationToken cancellationToken)
	{
		var basicProperties = await folder.GetBasicPropertiesAsync();
		if (!cancellationToken.IsCancellationRequested)
		{
			if (folder is ShortcutStorageFolder linkFolder)
			{
				return new ShortcutItem(folderViewViewModel, folder.FolderRelativeId)
				{
					PrimaryItemAttribute = StorageItemTypes.Folder,
					IsHiddenItem = false,
					Opacity = 1,
					FileImage = null!,
					LoadFileIcon = false,
					ItemNameRaw = folder.DisplayName,
					ItemDateModifiedReal = basicProperties.DateModified,
					ItemDateCreatedReal = folder.DateCreated,
					ItemType = folder.DisplayType,
					ItemPath = folder.Path,
					FileSize = null!,
					FileSizeBytes = 0,
					TargetPath = linkFolder.TargetPath,
					Arguments = linkFolder.Arguments,
					WorkingDirectory = linkFolder.WorkingDirectory,
					RunAsAdmin = linkFolder.RunAsAdmin
				};
			}
			else if (folder is BinStorageFolder binFolder)
			{
				return new RecycleBinItem(folderViewViewModel, folder.FolderRelativeId)
				{
					PrimaryItemAttribute = StorageItemTypes.Folder,
					ItemNameRaw = folder.DisplayName,
					ItemDateModifiedReal = basicProperties.DateModified,
					ItemDateCreatedReal = folder.DateCreated,
					ItemType = folder.DisplayType,
					IsHiddenItem = false,
					Opacity = 1,
					FileImage = null!,
					LoadFileIcon = false,
					ItemPath = string.IsNullOrEmpty(folder.Path) ? PathNormalization.Combine(currentStorageFolder.Path, folder.Name) : folder.Path,
					FileSize = basicProperties.Size.ToSizeString(),
					FileSizeBytes = (long)basicProperties.Size,
					ItemDateDeletedReal = binFolder.DateDeleted,
					ItemOriginalPath = binFolder.OriginalPath,
				};
			}
			else
			{
				return new ListedItem(folderViewViewModel, folder.FolderRelativeId)
				{
					PrimaryItemAttribute = StorageItemTypes.Folder,
					ItemNameRaw = folder.DisplayName,
					ItemDateModifiedReal = basicProperties.DateModified,
					ItemDateCreatedReal = folder.DateCreated,
					ItemType = folder.DisplayType,
					IsHiddenItem = false,
					Opacity = 1,
					FileImage = null!,
					LoadFileIcon = false,
					ItemPath = string.IsNullOrEmpty(folder.Path) ? PathNormalization.Combine(currentStorageFolder.Path, folder.Name) : folder.Path,
					FileSize = null!,
					FileSizeBytes = 0
				};
			}
		}

		return null!;
	}

	public static async Task<ListedItem> AddFileAsync(
        IFolderViewViewModel folderViewViewModel,
        BaseStorageFile file,
		StorageFolderWithPath currentStorageFolder,
		CancellationToken cancellationToken)
	{
		var basicProperties = await file.GetBasicPropertiesAsync();
		// Display name does not include extension
		var itemName = file.Name;
		var itemModifiedDate = basicProperties.DateModified;
		var itemCreatedDate = file.DateCreated;
		var itemPath = string.IsNullOrEmpty(file.Path) ? PathNormalization.Combine(currentStorageFolder.Path, file.Name) : file.Path;
		var itemSize = basicProperties.Size.ToSizeString();
		var itemSizeBytes = basicProperties.Size;
		var itemType = file.DisplayType;
		var itemFileExtension = file.FileType;
		var itemThumbnailImgVis = false;

		if (cancellationToken.IsCancellationRequested)
        {
            return null!;
        }

        // FILESTODO: is this needed to be handled here?
        if (App.LibraryManager.TryGetLibrary(file.Path, out var library))
		{
			return new LibraryItem(folderViewViewModel, library)
			{
				Opacity = 1,
				ItemDateModifiedReal = itemModifiedDate,
				ItemDateCreatedReal = itemCreatedDate,
			};
		}
		else
		{
			if (file is ShortcutStorageFile linkFile)
			{
				var isUrl = FileExtensionHelpers.IsWebLinkFile(linkFile.Name);
				return new ShortcutItem(folderViewViewModel, file.FolderRelativeId)
				{
					PrimaryItemAttribute = StorageItemTypes.File,
					FileExtension = itemFileExtension,
					IsHiddenItem = false,
					Opacity = 1,
					FileImage = null!,
					LoadFileIcon = itemThumbnailImgVis,
					ItemNameRaw = itemName,
					ItemDateModifiedReal = itemModifiedDate,
					ItemDateCreatedReal = itemCreatedDate,
					ItemType = itemType,
					ItemPath = itemPath,
					FileSize = itemSize,
					FileSizeBytes = (long)itemSizeBytes,
					TargetPath = linkFile.TargetPath,
					Arguments = linkFile.Arguments,
					WorkingDirectory = linkFile.WorkingDirectory,
					RunAsAdmin = linkFile.RunAsAdmin,
					IsUrl = isUrl,
				};
			}
			else if (file is BinStorageFile binFile)
			{
				return new RecycleBinItem(folderViewViewModel, file.FolderRelativeId)
				{
					PrimaryItemAttribute = StorageItemTypes.File,
					FileExtension = itemFileExtension,
					IsHiddenItem = false,
					Opacity = 1,
					FileImage = null!,
					LoadFileIcon = itemThumbnailImgVis,
					ItemNameRaw = itemName,
					ItemDateModifiedReal = itemModifiedDate,
					ItemDateCreatedReal = itemCreatedDate,
					ItemType = itemType,
					ItemPath = itemPath,
					FileSize = itemSize,
					FileSizeBytes = (long)itemSizeBytes,
					ItemDateDeletedReal = binFile.DateDeleted,
					ItemOriginalPath = binFile.OriginalPath
				};
			}
			else
			{
				return new ListedItem(folderViewViewModel, file.FolderRelativeId)
				{
					PrimaryItemAttribute = StorageItemTypes.File,
					FileExtension = itemFileExtension,
					IsHiddenItem = false,
					Opacity = 1,
					FileImage = null!,
					LoadFileIcon = itemThumbnailImgVis,
					ItemNameRaw = itemName,
					ItemDateModifiedReal = itemModifiedDate,
					ItemDateCreatedReal = itemCreatedDate,
					ItemType = itemType,
					ItemPath = itemPath,
					FileSize = itemSize,
					FileSizeBytes = (long)itemSizeBytes,
				};
			}
		}
	}
}
