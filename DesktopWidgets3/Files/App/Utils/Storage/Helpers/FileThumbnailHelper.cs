// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Utils.Shell;

namespace Files.App.Utils.Storage;

public class FileThumbnailHelper
{
    public static Task<(byte[] IconData, byte[] OverlayData)> LoadIconAndOverlayAsync(string filePath, uint thumbnailSize, bool isFolder = false)
    {
        return Win32API.StartSTATask(() => Win32API.GetFileIconAndOverlay(filePath, (int)thumbnailSize, isFolder, true, false))!;
    }

    public static async Task<byte[]> LoadOverlayAsync(string filePath, uint thumbnailSize)
    {
        return (await Win32API.StartSTATask(() => Win32API.GetFileIconAndOverlay(filePath, (int)thumbnailSize, false, true, true))).overlay;
    }

    public static async Task<byte[]> LoadIconWithoutOverlayAsync(string filePath, uint thumbnailSize, bool isFolder = false)
    {
        return (await Win32API.StartSTATask(() => Win32API.GetFileIconAndOverlay(filePath, (int)thumbnailSize, isFolder, false))).icon;
    }
}
