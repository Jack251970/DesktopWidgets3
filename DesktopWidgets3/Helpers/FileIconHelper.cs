namespace DesktopWidgets3.Helpers;

/// <summary>
/// https://github.com/files-community/Files/blob/main/src/Files.App/Utils/Storage/Helpers/FileThumbnailHelper.cs
/// </summary>
public static class FileIconHelper
{
    public static Task<(byte[]? IconData, byte[]? OverlayData)> LoadIconAndOverlayAsync(string filePath, uint thumbnailSize, bool isFolder = false) 
        => Win32API.StartSTATask(() => Win32API.GetFileIconAndOverlay(filePath, (int)thumbnailSize, isFolder, true, false));
}
