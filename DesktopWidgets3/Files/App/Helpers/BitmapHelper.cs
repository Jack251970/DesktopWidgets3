// Copyright(c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Media.Imaging;

namespace Files.App.Helpers;

internal static class BitmapHelper
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
