﻿// Copyright(c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Utils.Storage;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.Storage;

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

    /// <summary>
    /// Rotates the image at the specified file path.
    /// </summary>
    /// <param name="filePath">The file path to the image.</param>
    /// <param name="rotation">The rotation direction.</param>
    /// <remarks>
    /// https://learn.microsoft.com/uwp/api/windows.graphics.imaging.bitmapdecoder?view=winrt-22000
    /// https://learn.microsoft.com/uwp/api/windows.graphics.imaging.bitmapencoder?view=winrt-22000
    /// </remarks>
    public static async Task RotateAsync(string filePath, BitmapRotation rotation)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return;
        }

        var file = await StorageHelpers.ToStorageItem<IStorageFile>(filePath);
        if (file is null)
        {
            return;
        }

        var fileStreamRes = await FilesystemTasks.Wrap(() => file.OpenAsync(FileAccessMode.ReadWrite).AsTask());
        using var fileStream = fileStreamRes.Result;
        if (fileStream is null)
        {
            return;
        }

        var decoder = await BitmapDecoder.CreateAsync(fileStream);
        using var memStream = new InMemoryRandomAccessStream();
        var encoder = await BitmapEncoder.CreateForTranscodingAsync(memStream, decoder);

        for (var i = 0; i < decoder.FrameCount - 1; i++)
        {
            encoder.BitmapTransform.Rotation = rotation;
            await encoder.GoToNextFrameAsync();
        }

        encoder.BitmapTransform.Rotation = rotation;

        await encoder.FlushAsync();

        memStream.Seek(0);
        fileStream.Seek(0);
        fileStream.Size = 0;

        await RandomAccessStream.CopyAsync(memStream, fileStream);
    }

    /// <summary>
    /// This function encodes a software bitmap with the specified encoder and saves it to a file
    /// </summary>
    /// <param name="softwareBitmap"></param>
    /// <param name="outputFile"></param>
    /// <param name="encoderId">The guid of the image encoder type</param>
    /// <returns></returns>
    public static async Task SaveSoftwareBitmapToFileAsync(SoftwareBitmap softwareBitmap, BaseStorageFile outputFile, Guid encoderId)
    {
        using var stream = await outputFile.OpenAsync(FileAccessMode.ReadWrite);
        // Create an encoder with the desired format
        var encoder = await BitmapEncoder.CreateAsync(encoderId, stream);

        // Set the software bitmap
        encoder.SetSoftwareBitmap(softwareBitmap);

        try
        {
            await encoder.FlushAsync();
        }
        catch (Exception err)
        {
            const int WINCODEC_ERR_UNSUPPORTEDOPERATION = unchecked((int)0x88982F81);
            switch (err.HResult)
            {
                case WINCODEC_ERR_UNSUPPORTEDOPERATION:
                    // If the encoder does not support writing a thumbnail, then try again
                    // but disable thumbnail generation.
                    encoder.IsThumbnailGenerated = false;
                    break;

                default:
                    throw;
            }
        }

        if (encoder.IsThumbnailGenerated == false)
        {
            await encoder.FlushAsync();
        }
    }
}
