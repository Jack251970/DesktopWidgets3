// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using System.IO;
using ByteSize = ByteSizeLib.ByteSize;

namespace Files.App.ViewModels.Properties;

internal sealed class FolderProperties : BaseProperties
{
    private readonly IFolderViewViewModel FolderViewViewModel;

	public ListedItem Item { get; }

	public FolderProperties(
		SelectedItemsPropertiesViewModel viewModel,
		CancellationTokenSource tokenSource,
		DispatcherQueue coreDispatcher,
		ListedItem item,
		IShellPage instance)
	{
        FolderViewViewModel = item.FolderViewViewModel;

		ViewModel = viewModel;
		TokenSource = tokenSource;
		Dispatcher = coreDispatcher;
		Item = item;
		AppInstance = instance;

		GetBaseProperties();

		ViewModel.PropertyChanged += ViewModel_PropertyChanged;
	}

	public override void GetBaseProperties()
	{
		if (Item is not null)
		{
			ViewModel.ItemName = Item.Name;
			ViewModel.OriginalItemName = Item.Name;
			ViewModel.ItemType = Item.ItemType;
			ViewModel.ItemLocation = (Item as RecycleBinItem)?.ItemOriginalFolder ??
				(Path.IsPathRooted(Item.ItemPath) ? Path.GetDirectoryName(Item.ItemPath) : Item.ItemPath)!;
			ViewModel.ItemModifiedTimestampReal = Item.ItemDateModifiedReal;
			ViewModel.ItemCreatedTimestampReal = Item.ItemDateCreatedReal;
			ViewModel.LoadCustomIcon = Item.LoadCustomIcon;
			ViewModel.CustomIconSource = Item.CustomIconSource;
			ViewModel.LoadFileIcon = Item.LoadFileIcon;
			ViewModel.ContainsFilesOrFolders = Item.ContainsFilesOrFolders;

			if (Item.IsShortcut)
			{
				var shortcutItem = (ShortcutItem)Item;
				ViewModel.ShortcutItemType = "Folder".GetLocalizedResource();
				ViewModel.ShortcutItemPath = shortcutItem.TargetPath;
				ViewModel.IsShortcutItemPathReadOnly = false;
				ViewModel.ShortcutItemWorkingDir = shortcutItem.WorkingDirectory;
				ViewModel.ShortcutItemWorkingDirVisibility = false;
				ViewModel.ShortcutItemArguments = shortcutItem.Arguments;
				ViewModel.ShortcutItemArgumentsVisibility = false;
				ViewModel.ShortcutItemOpenLinkCommand = new RelayCommand(async () =>
				{
					await ThreadExtensions.MainDispatcherQueue.EnqueueOrInvokeAsync(
                        () => NavigationHelpers.OpenPathInNewTab(FolderViewViewModel, Path.GetDirectoryName(Environment.ExpandEnvironmentVariables(ViewModel.ShortcutItemPath)), true));
                },
                () =>
				{
					return !string.IsNullOrWhiteSpace(ViewModel.ShortcutItemPath);
				});
			}
		}
	}

	public async override Task GetSpecialPropertiesAsync()
	{
        ViewModel.IsHidden = Win32Helper.HasFileAttribute(
                Item.ItemPath, FileAttributes.Hidden);

        var result = await FileThumbnailHelper.GetIconAsync(
            Item.ItemPath,
            Constants.ShellIconSizes.ExtraLarge,
            true,
            IconOptions.UseCurrentScale);

        if (result is not null)
        {
            ViewModel.IconData = result;
            ViewModel.LoadFolderGlyph = false;
            ViewModel.LoadFileIcon = true;
        }

        if (Item.IsShortcut)
		{
			ViewModel.ItemSizeVisibility = true;
			ViewModel.ItemSize = Item.FileSizeBytes.ToLongSizeString();

			// Only load the size for items on the device
			if (Item.SyncStatusUI.SyncStatus is not CloudDriveSyncStatus.FileOnline and not CloudDriveSyncStatus.FolderOnline)
            {
                ViewModel.ItemSizeOnDisk = Win32Helper.GetFileSizeOnDisk(Item.ItemPath)?.ToLongSizeString() ??
                    string.Empty;
            }

            ViewModel.ItemCreatedTimestampReal = Item.ItemDateCreatedReal;
			ViewModel.ItemAccessedTimestampReal = Item.ItemDateAccessedReal;
			if (Item.IsLinkItem || string.IsNullOrWhiteSpace(((ShortcutItem)Item).TargetPath))
			{
				// Can't show any other property
				return;
			}
		}

		var folderPath = (Item as ShortcutItem)?.TargetPath ?? Item.ItemPath;
		BaseStorageFolder storageFolder = await AppInstance.FilesystemViewModel.GetFolderFromPathAsync(folderPath);

		if (storageFolder is not null)
		{
			ViewModel.ItemCreatedTimestampReal = storageFolder.DateCreated;
			if (storageFolder.Properties is not null)
            {
                _ = GetOtherPropertiesAsync(storageFolder.Properties);
            }

            // Only load the size for items on the device
            if (Item.SyncStatusUI.SyncStatus is not CloudDriveSyncStatus.FileOnline and not 
				CloudDriveSyncStatus.FolderOnline and not
				CloudDriveSyncStatus.FolderOfflinePartial)
            {
                _ = GetFolderSizeAsync(storageFolder.Path, TokenSource.Token);
            }
        }
		else if (Item.ItemPath.Equals(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
		{
			var (_, NumItems, BinSize) = Win32Helper.QueryRecycleBin();
            if (BinSize is long binSize)
			{
				ViewModel.ItemSizeBytes = binSize;
				ViewModel.ItemSize = ByteSize.FromBytes(binSize).ToString();
				ViewModel.ItemSizeVisibility = true;
			}
			else
			{
				ViewModel.ItemSizeVisibility = false;
			}
			ViewModel.ItemSizeOnDisk = string.Empty;
			if (NumItems is long numItems)
			{
				ViewModel.FilesCount = (int)numItems;
				SetItemsCountString();
				ViewModel.FilesAndFoldersCountVisibility = true;
			}
			else
			{
				ViewModel.FilesAndFoldersCountVisibility = false;
			}

			ViewModel.ItemCreatedTimestampVisibility = false;
			ViewModel.ItemAccessedTimestampVisibility = false;
			ViewModel.ItemModifiedTimestampVisibility = false;
			ViewModel.LastSeparatorVisibility = false;
		}
		else
		{
			_ = GetFolderSizeAsync(folderPath, TokenSource.Token);
		}
	}

	private async Task GetFolderSizeAsync(string folderPath, CancellationToken token)
	{
		if (string.IsNullOrEmpty(folderPath))
		{
			// In MTP devices calculating folder size would be too slow
			// Also should use StorageFolder methods instead of FindFirstFileExFromApp
			return;
		}

		ViewModel.ItemSizeVisibility = true;
		ViewModel.ItemSizeProgressVisibility = true;
		ViewModel.ItemSizeOnDiskProgressVisibility = true;

		var fileSizeTask = Task.Run(async () =>
		{
			var size = await CalculateFolderSizeAsync(folderPath, token);
			return size;
		});

		try
		{
			var (size, sizeOnDisk) = await fileSizeTask;
			ViewModel.ItemSizeBytes = size;
			ViewModel.ItemSize = size.ToLongSizeString();
			ViewModel.ItemSizeOnDiskBytes = sizeOnDisk;
			ViewModel.ItemSizeOnDisk = sizeOnDisk.ToLongSizeString();
		}
		catch (Exception ex)
		{
			App.Logger.LogWarning(ex, ex.Message);
		}

		ViewModel.ItemSizeProgressVisibility = false;
		ViewModel.ItemSizeOnDiskProgressVisibility = false;

		SetItemsCountString();
	}

	private async void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case "IsHidden":
                if (ViewModel.IsHidden is not null)
                {
                    if ((bool)ViewModel.IsHidden)
                    {
                        Win32Helper.SetFileAttribute(Item.ItemPath, FileAttributes.Hidden);
                    }
                    else
                    {
                        Win32Helper.UnsetFileAttribute(Item.ItemPath, FileAttributes.Hidden);
                    }
                }
                break;

			case "ShortcutItemPath":
			case "ShortcutItemWorkingDir":
			case "ShortcutItemArguments":
				var tmpItem = (ShortcutItem)Item;

				if (string.IsNullOrWhiteSpace(ViewModel.ShortcutItemPath))
                {
                    return;
                }

                await FileOperationsHelpers.CreateOrUpdateLinkAsync(Item.ItemPath, ViewModel.ShortcutItemPath, ViewModel.ShortcutItemArguments, ViewModel.ShortcutItemWorkingDir, tmpItem.RunAsAdmin);
				break;
		}
	}
}
