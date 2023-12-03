using Files.App.Utils.Shell;
using Microsoft.UI.Xaml.Media.Imaging;

namespace DesktopWidgets3.Helpers;

/// <summary>
/// Provides static helper for get icon and overlay from file.
/// </summary>
public static class FileIconHelper
{
    public static async Task<(BitmapImage? Icon, BitmapImage? Overlay)> GetFileIconAndOverlayAsync(string filePath, bool isFolder, uint thumbnailSize = 96)
    {
        var (iconData, overlayData) = await Win32API.StartSTATask(() => Win32API.GetFileIconAndOverlay(filePath, (int)thumbnailSize, isFolder, true, false));

        return (iconData is null ? null : await iconData.ToBitmapAsync(), overlayData is null ? null : await overlayData.ToBitmapAsync());
    }

    public static async Task<BitmapImage?> GetFileOverlayAsync(string filePath, uint thumbnailSize = 96)
    {
        var overlayData = (await Win32API.StartSTATask(() => Win32API.GetFileIconAndOverlay(filePath, (int)thumbnailSize, false, true, true))).overlay;
        
        return overlayData is null ? null : await overlayData.ToBitmapAsync();
    }

    public static async Task<BitmapImage?> GetFileIconWithoutOverlayAsync(string filePath, bool isFolder = false, uint thumbnailSize = 96)
    {
        var iconData = (await Win32API.StartSTATask(() => Win32API.GetFileIconAndOverlay(filePath, (int)thumbnailSize, isFolder, false, false))).icon;

        return iconData is null ? null : await iconData.ToBitmapAsync();
    }

    private static async Task<BitmapImage?> ToBitmapAsync(this byte[]? data, int decodeSize = -1)
    {
        if (data is null)
        {
            return null;
        }

        try
        {
            using var ms = new MemoryStream(data);
            var image = new BitmapImage();
            if (decodeSize > 0)
            {
                image.DecodePixelWidth = decodeSize;
                image.DecodePixelHeight = decodeSize;
            }
            await image.SetSourceAsync(ms.AsRandomAccessStream());
            return image;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
