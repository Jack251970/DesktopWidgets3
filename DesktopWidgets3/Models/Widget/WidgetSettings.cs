namespace DesktopWidgets3.Models.Widget;

public class BaseWidgetSettings
{

}

public class ClockWidgetSettings : BaseWidgetSettings
{
    public bool ShowSeconds { get; set; } = true;
}

public class FolderViewWidgetSettings : BaseWidgetSettings
{
    public string FolderPath { get; set; } = string.Empty;

    public bool ShowIconOverlay { get; set; } = true;
}