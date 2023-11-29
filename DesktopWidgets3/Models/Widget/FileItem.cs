using Microsoft.UI.Xaml.Media.Imaging;

namespace DesktopWidgets3.Models.Widget;

public class BaseFileItem
{
    public required string FileName
    {
        get; set;
    }

    public required string FilePath
    {
        get; set;
    }
}

public class FolderViewFileItem : BaseWidgetItem
{
    public required string FileName
    {
        get; set;
    }

    public required string FilePath
    {
        get; set;
    }

    public BitmapImage? FileIcon
    {
        get; set;
    }

    /*public Action<FolderViewItem>? FileClickCallback
    {
        get; set;
    }*/
}