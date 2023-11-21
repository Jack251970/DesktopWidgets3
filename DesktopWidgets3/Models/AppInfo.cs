using Microsoft.UI.Xaml.Media.Imaging;

namespace DesktopWidgets3.Models;

public class AppInfo
{
    public BitmapImage? AppIcon
    {
        get; set;
    }

    public required string AppPath
    {
        get; set;
    }

    public required string AppName
    {
        get; set;
    }

    public required bool IsBlock
    {
        get; set;
    }

    public required string ExeName
    {
        get; set;
    }

    public required int ListIndex
    {
        get; set;
    }
}