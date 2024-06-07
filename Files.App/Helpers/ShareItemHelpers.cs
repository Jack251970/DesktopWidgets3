// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Storage;

namespace Files.App.Helpers;

public static class ShareItemHelpers
{
	public static bool IsItemShareable(ListedItem item)
		=> !item.IsHiddenItem &&
			(!item.IsShortcut || item.IsLinkItem) &&
			(item.PrimaryItemAttribute != StorageItemTypes.Folder || item.IsArchive);

    public static async Task ShareItemsAsync(IFolderViewViewModel folderViewViewModel, IEnumerable<ListedItem> itemsToShare)
	{
        if (itemsToShare is null)
        {
            return;
        }

        var interop = DataTransferManager.As<IDataTransferManagerInterop>();
        var result = interop.GetForWindow(folderViewViewModel.WindowHandle, Win32PInvoke.DataTransferManagerInteropIID);

        var manager = WinRT.MarshalInterface<DataTransferManager>.FromAbi(result);
		manager.DataRequested += new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(Manager_DataRequested);

        try
        {
            interop.ShowShareUIForWindow(folderViewViewModel.WindowHandle);
        }
        catch (Exception ex)
        {
            var errorDialog = new ContentDialog()
            {
                Title = "FaildToShareItems".GetLocalizedResource(),
                Content = ex.Message,
                PrimaryButtonText = "OK".GetLocalizedResource(),
            };

            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                errorDialog.XamlRoot = folderViewViewModel.XamlRoot;
            }

            await errorDialog.TryShowAsync(folderViewViewModel);
        }

		async void Manager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
		{
			var dataRequestDeferral = args.Request.GetDeferral();
            List<IStorageItem> items = [];
            var dataRequest = args.Request;

			foreach (var item in itemsToShare)
			{
				if (item is ShortcutItem shItem)
				{
					if (shItem.IsLinkItem && !string.IsNullOrEmpty(shItem.TargetPath))
					{
						dataRequest.Data.Properties.Title = string.Format("ShareDialogTitle".GetLocalizedResource(), item.Name);
						dataRequest.Data.Properties.Description = "ShareDialogSingleItemDescription".GetLocalizedResource();
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
				dataRequest.Data.Properties.Title = string.Format("ShareDialogTitle".GetLocalizedResource(), items.First().Name);
				dataRequest.Data.Properties.Description = "ShareDialogSingleItemDescription".GetLocalizedResource();
			}
			else if (items.Count == 0)
			{
				dataRequest.FailWithDisplayText("ShareDialogFailMessage".GetLocalizedResource());
				dataRequestDeferral.Complete();

				return;
			}
			else
			{
				dataRequest.Data.Properties.Title = string.Format(
					"ShareDialogTitleMultipleItems".GetLocalizedResource(),
					items.Count,
					"ItemsCount.Text".GetLocalizedResource());
				dataRequest.Data.Properties.Description = "ShareDialogMultipleItemsDescription".GetLocalizedResource();
			}

			dataRequest.Data.SetStorageItems(items, false);
			dataRequestDeferral.Complete();
		}
	}
}
