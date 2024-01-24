// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;

namespace Files.App.Helpers;

public static class UIHelpers
{
	/*public static event PropertyChangedEventHandler? PropertyChanged;

	private static bool canShowDialog = true;
	public static bool CanShowDialog
	{
		get => canShowDialog;
		private set
		{
			if (value == canShowDialog)
            {
                return;
            }

            canShowDialog = value;
			PropertyChanged?.Invoke(null, new PropertyChangedEventArgs(nameof(CanShowDialog)));
		}
	}*/

	/*/// <summary>
	/// Displays a toast or dialog to indicate the result of
	/// a device ejection operation.
	/// </summary>
	/// <param name="type">Type of drive to eject</param>
	/// <param name="result">Only true implies a successful device ejection</param>
	/// <returns></returns>
	public static async Task ShowDeviceEjectResultAsync(IFolderViewViewModel viewModel, Data.Items.DriveType type, bool result)
	{
		if (type != Data.Items.DriveType.CDRom && result)
		{
			Debug.WriteLine("Device successfully ejected");

			var toastContent = new ToastContent()
			{
				Visual = new ToastVisual()
				{
					BindingGeneric = new ToastBindingGeneric()
					{
						Children =
						{
							new AdaptiveText()
							{
								Text = "EjectNotificationHeader".ToLocalized()
							},
							new AdaptiveText()
							{
								Text = "EjectNotificationBody".ToLocalized()
							}
						},
						Attribution = new ToastGenericAttributionText()
						{
							Text = "SettingsAboutAppName".ToLocalized()
						}
					}
				},
				ActivationType = ToastActivationType.Protocol
			};

			// Create the toast notification
			var toastNotif = new ToastNotification(toastContent.GetXml());

			// And send the notification
			ToastNotificationManager.CreateToastNotifier().Show(toastNotif);
		}
		else if (!result)
		{
			Debug.WriteLine("Can't eject device");

            await DialogDisplayHelper.ShowDialogAsync(
                viewModel,
                "EjectNotificationErrorDialogHeader".ToLocalized(),
                "EjectNotificationErrorDialogBody".ToLocalized());
        }
	}*/

	public static async Task<ContentDialogResult> TryShowAsync(this ContentDialog dialog, IFolderViewViewModel viewModel)
	{
		if (!viewModel.CanShowDialog)
        {
            return ContentDialogResult.None;
        }

        try
		{
            viewModel.CanShowDialog = false;
			return await SetContentDialogRoot(dialog, viewModel).ShowAsync();
		}
		catch // A content dialog is already open
		{
			return ContentDialogResult.None;
		}
		finally
		{
            viewModel.CanShowDialog = true;
		}
	}

	public static async Task<DialogResult> TryShowAsync<TViewModel>(this IDialog<TViewModel> dialog, IFolderViewViewModel viewModel)
		where TViewModel : class, INotifyPropertyChanged
	{
		return (DialogResult)await ((ContentDialog)dialog).TryShowAsync(viewModel);
	}

	// WINUI3
	private static ContentDialog SetContentDialogRoot(ContentDialog contentDialog, IFolderViewViewModel viewModel)
	{
		if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
		{
			contentDialog.XamlRoot = viewModel.MainWindow.Content.XamlRoot;
		}
		return contentDialog;
	}

	public static void CloseAllDialogs(IFolderViewViewModel viewModel)
	{
		var openedDialogs = VisualTreeHelper.GetOpenPopupsForXamlRoot(viewModel.MainWindow.Content.XamlRoot);

		foreach (var item in openedDialogs)
		{
			if (item.Child is ContentDialog dialog)
			{
				dialog.Hide();
			}
		}
	}

	private static readonly IEnumerable<IconFileInfo> SidebarIconResources = LoadSidebarIconResources();

	private static readonly IconFileInfo ShieldIconResource = LoadShieldIconResource();

	public static IconFileInfo GetSidebarIconResourceInfo(int index)
	{
		var icons = SidebarIconResources;
		return icons?.FirstOrDefault(x => x.Index == index)!;
	}

	public static async Task<BitmapImage?> GetSidebarIconResource(int index)
	{
		var iconInfo = GetSidebarIconResourceInfo(index);

		return iconInfo is not null
			? await iconInfo.IconData.ToBitmapAsync()
			: null;
	}

	public static async Task<BitmapImage?> GetShieldIconResource()
	{
		return ShieldIconResource is not null
			? await ShieldIconResource.IconData.ToBitmapAsync()
			: null;
	}

	private static IEnumerable<IconFileInfo> LoadSidebarIconResources()
	{
		var imageres = Path.Combine(Constants.UserEnvironmentPaths.SystemRootPath, "System32", "imageres.dll");
		var imageResList = Win32API.ExtractSelectedIconsFromDLL(imageres, new List<int>() {
				Constants.ImageRes.RecycleBin,
				Constants.ImageRes.NetworkDrives,
				Constants.ImageRes.Libraries,
				Constants.ImageRes.ThisPC,
				Constants.ImageRes.CloudDrives,
				Constants.ImageRes.Folder,
				Constants.ImageRes.OneDrive
			}, 32);

		return imageResList;
	}

	private static IconFileInfo LoadShieldIconResource()
	{
		var imageres = Path.Combine(Constants.UserEnvironmentPaths.SystemRootPath, "System32", "imageres.dll");
		var imageResList = Win32API.ExtractSelectedIconsFromDLL(imageres, new List<int>() {
				Constants.ImageRes.ShieldIcon
			}, 16);

		return imageResList.First();
	}
}