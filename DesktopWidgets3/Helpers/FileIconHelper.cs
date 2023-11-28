using Microsoft.UI.Xaml.Media.Imaging;

namespace DesktopWidgets3.Helpers;

public static class FileIconHelper
{
    public static async Task<(BitmapImage? Icon, BitmapImage? Overlay)> GetFileIconAndOverlayAsync(string filePath, bool isFolder)
    {
        var (iconData, overlayData) = await Win32API.StartSTATask(() => Win32API.GetFileIconAndOverlay(filePath, 96, isFolder, true, false));

        return (iconData is null ? null : await iconData.ToBitmapAsync(), overlayData is null ? null : await overlayData.ToBitmapAsync());
    }
}
