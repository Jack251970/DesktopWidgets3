namespace DesktopWidgets3.Helpers;

public static class FileIconHelper
{
    public static Task<(byte[]? IconData, byte[]? OverlayData)> LoadIconAndOverlayAsync(string filePath, uint thumbnailSize, bool isFolder = false) 
        => Win32API.StartSTATask(() => Win32API.GetFileIconAndOverlay(filePath, (int)thumbnailSize, isFolder, true, false));
}
