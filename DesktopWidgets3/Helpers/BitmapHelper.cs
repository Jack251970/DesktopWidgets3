using Microsoft.UI.Xaml.Media.Imaging;

namespace DesktopWidgets3.Helpers;

/// <summary>
/// https://github.com/files-community/Files/blob/main/src/Files.App/Helpers/BitmapHelper.cs
/// </summary>
public static class BitmapHelper
{
    public static async Task<BitmapImage?> ToBitmapAsync(this byte[]? data, int decodeSize = -1)
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
