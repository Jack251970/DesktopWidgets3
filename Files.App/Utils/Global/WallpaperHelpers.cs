// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using Vanara.PInvoke;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.System.UserProfile;

namespace Files.App.Utils;

public static class WallpaperHelpers
{
	public static async Task SetAsBackgroundAsync(IFolderViewViewModel folderViewViewModel, WallpaperType type, string filePath)
	{
        try
        {
            if (type == WallpaperType.Desktop)
            {
                // Set the desktop background
                var wallpaper = (Shell32.IDesktopWallpaper)new Shell32.DesktopWallpaper();
                wallpaper.GetMonitorDevicePathAt(0, out var monitorId);
                wallpaper.SetWallpaper(monitorId, filePath);
            }
            else if (type == WallpaperType.LockScreen)
            {
                // Set the lockscreen background
                IStorageFile sourceFile = await StorageFile.GetFileFromPathAsync(filePath);
                await LockScreen.SetImageFileAsync(sourceFile);
            }
        }
        catch (Exception ex)
        {
            ShowErrorPrompt(folderViewViewModel, ex.Message);
        }
    }

	public static void SetSlideshow(IFolderViewViewModel folderViewViewModel, string[] filePaths)
	{
		if (filePaths is null || !filePaths.Any())
        {
            return;
        }

        try
		{
			var idList = filePaths.Select(Shell32.IntILCreateFromPath).ToArray();
			Shell32.SHCreateShellItemArrayFromIDLists((uint)idList.Length, idList.ToArray(), out var shellItemArray);

			// Set SlideShow
			var wallpaper = (Shell32.IDesktopWallpaper)new Shell32.DesktopWallpaper();
			wallpaper.SetSlideshow(shellItemArray);

			// Set wallpaper to fill desktop.
			wallpaper.SetPosition(Shell32.DESKTOP_WALLPAPER_POSITION.DWPOS_FILL);
		}
		catch (Exception ex)
		{
			ShowErrorPrompt(folderViewViewModel, ex.Message);
		}
	}

	private static async void ShowErrorPrompt(IFolderViewViewModel folderViewViewModel, string exception)
	{
		var errorDialog = new ContentDialog()
		{
			Title = "FailedToSetBackground".GetLocalizedResource(),
			Content = exception,
			PrimaryButtonText = "OK".GetLocalizedResource(),
		};

		if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
        {
            errorDialog.XamlRoot = folderViewViewModel.XamlRoot;
        }

        await errorDialog.TryShowAsync(folderViewViewModel);
	}
}

