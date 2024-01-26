﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage;

namespace Files.App.Helpers;

public static class ShareItemHelpers
{
	public static bool IsItemShareable(ListedItem item)
		=> !item.IsHiddenItem &&
			(!item.IsShortcut || item.IsLinkItem) &&
			(item.PrimaryItemAttribute != StorageItemTypes.Folder || item.IsArchive);

	public static void ShareItems(IFolderViewViewModel folderViewViewModel, IEnumerable<ListedItem> itemsToShare)
	{
		var interop = DataTransferManager.As<IDataTransferManagerInterop>();
		var result = interop.GetForWindow(folderViewViewModel.WindowHandle, InteropHelpers.DataTransferManagerInteropIID);

		var manager = WinRT.MarshalInterface<DataTransferManager>.FromAbi(result);
		manager.DataRequested += new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(Manager_DataRequested);

		interop.ShowShareUIForWindow(folderViewViewModel.WindowHandle);

		async void Manager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
		{
			var dataRequestDeferral = args.Request.GetDeferral();
			List<IStorageItem> items = new();
			var dataRequest = args.Request;

			foreach (var item in itemsToShare)
			{
				if (item is ShortcutItem shItem)
				{
					if (shItem.IsLinkItem && !string.IsNullOrEmpty(shItem.TargetPath))
					{
						dataRequest.Data.Properties.Title = string.Format("ShareDialogTitle".ToLocalized(), item.Name);
						dataRequest.Data.Properties.Description = "ShareDialogSingleItemDescription".ToLocalized();
						dataRequest.Data.SetWebLink(new Uri(shItem.TargetPath));
						dataRequestDeferral.Complete();

						return;
					}
				}
				else if (item.PrimaryItemAttribute == StorageItemTypes.Folder && !item.IsArchive)
				{
					if (await StorageHelpers.ToStorageItem<BaseStorageFolder>(item.ItemPath) is BaseStorageFolder folder)
                    {
                        items.Add(folder);
                    }
                }
				else
				{
					if (await StorageHelpers.ToStorageItem<BaseStorageFile>(item.ItemPath) is BaseStorageFile file)
                    {
                        items.Add(file);
                    }
                }
			}

			if (items.Count == 1)
			{
				dataRequest.Data.Properties.Title = string.Format("ShareDialogTitle".ToLocalized(), items.First().Name);
				dataRequest.Data.Properties.Description = "ShareDialogSingleItemDescription".ToLocalized();
			}
			else if (items.Count == 0)
			{
				dataRequest.FailWithDisplayText("ShareDialogFailMessage".ToLocalized());
				dataRequestDeferral.Complete();

				return;
			}
			else
			{
				dataRequest.Data.Properties.Title = string.Format(
					"ShareDialogTitleMultipleItems".ToLocalized(),
					items.Count,
					"ItemsCount.Text".ToLocalized());
				dataRequest.Data.Properties.Description = "ShareDialogMultipleItemsDescription".ToLocalized();
			}

			dataRequest.Data.SetStorageItems(items, false);
			dataRequestDeferral.Complete();
		}
	}
}
