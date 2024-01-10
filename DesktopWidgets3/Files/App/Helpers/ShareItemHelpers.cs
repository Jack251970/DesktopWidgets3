// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.Helpers;
using DesktopWidgets3.ViewModels.Pages.Widget;
using Files.App.Utils;
using Files.App.Utils.Storage;
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

	public static void ShareItems(FolderViewViewModel viewModel, IEnumerable<ListedItem> itemsToShare)
	{
		var interop = DataTransferManager.As<IDataTransferManagerInterop>();
		var result = interop.GetForWindow(viewModel.WidgetWindow.WindowHandle, InteropHelpers.DataTransferManagerInteropIID);

		var manager = WinRT.MarshalInterface<DataTransferManager>.FromAbi(result);
		manager.DataRequested += new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(Manager_DataRequested);

		interop.ShowShareUIForWindow(viewModel.WidgetWindow.WindowHandle);

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
						dataRequest.Data.Properties.Title = string.Format("ShareDialogTitle".GetLocalized(), item.Name);
						dataRequest.Data.Properties.Description = "ShareDialogSingleItemDescription".GetLocalized();
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
				dataRequest.Data.Properties.Title = string.Format("ShareDialogTitle".GetLocalized(), items.First().Name);
				dataRequest.Data.Properties.Description = "ShareDialogSingleItemDescription".GetLocalized();
			}
			else if (items.Count == 0)
			{
				dataRequest.FailWithDisplayText("ShareDialogFailMessage".GetLocalized());
				dataRequestDeferral.Complete();

				return;
			}
			else
			{
				dataRequest.Data.Properties.Title = string.Format(
					"ShareDialogTitleMultipleItems".GetLocalized(),
					items.Count,
					"ItemsCount.Text".GetLocalized());
				dataRequest.Data.Properties.Description = "ShareDialogMultipleItemsDescription".GetLocalized();
			}

			dataRequest.Data.SetStorageItems(items, false);
			dataRequestDeferral.Complete();
		}
	}
}
