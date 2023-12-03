using Microsoft.UI.Xaml.Media.Imaging;

namespace DesktopWidgets3.Models.Widget.FolderView;

public class BaseFileItem
{
    public required string FileName { get; set; }

    public required string FilePath { get; set; }
}

public class FolderViewFileItem : BaseWidgetItem
{
    public required string FileName { get; set; }

    public required string FilePath { get; set; }

    public required FileType FileType { get; set; }

    public BitmapImage? Icon { get; set; } = null;

    public BitmapImage? IconOverlay { get; set; } = null;
}