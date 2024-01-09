// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using DesktopWidgets3.Files.App.Data.Models;
using DesktopWidgets3.Files.App.Helpers;
using DesktopWidgets3.Files.App.Utils.Storage;
using DesktopWidgets3.Files.Core.Services;
using DesktopWidgets3.Files.Core.Storage;
using DesktopWidgets3.Files.Core.Storage.LocatableStorage;
using DesktopWidgets3.Files.Shared.Utils;
using Windows.Storage.FileProperties;

namespace DesktopWidgets3.Files.App.Services;

internal sealed class ImagingService : IImagingService
{
    public async Task<IImage?> GetIconAsync(IStorable storable, CancellationToken cancellationToken)
    {
        if (storable is not ILocatableStorable locatableStorable)
        {
            return null;
        }

        var iconData = await FileThumbnailHelper.LoadIconFromPathAsync(locatableStorable.Path, 24u, ThumbnailMode.ListView, ThumbnailOptions.ResizeThumbnail);
        if (iconData is null)
        {
            return null;
        }

        var bitmapImage = await iconData.ToBitmapAsync();
        return new BitmapImageModel(bitmapImage!);
    }

    public async Task<IImage?> GetImageModelFromDataAsync(byte[]? rawData)
    {
        return new BitmapImageModel((await BitmapHelper.ToBitmapAsync(rawData))!);
    }

    public async Task<IImage?> GetImageModelFromPathAsync(string filePath, uint thumbnailSize = 64)
    {
        if (await FileThumbnailHelper.LoadIconFromPathAsync(filePath, thumbnailSize, ThumbnailMode.ListView, ThumbnailOptions.ResizeThumbnail) is byte[] imageBuffer)
        {
            return await GetImageModelFromDataAsync(imageBuffer);
        }

        return null;
    }
}
