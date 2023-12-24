namespace DesktopWidgets3.Models.Widget;

public class BaseWidgetSettings
{
    public virtual BaseWidgetSettings Clone()
    {
        return (BaseWidgetSettings)MemberwiseClone();
    }
}

public class ClockWidgetSettings : BaseWidgetSettings
{
    public bool ShowSeconds { get; set; } = true;

    public override BaseWidgetSettings Clone()
    {
        var clone = (ClockWidgetSettings)base.Clone();
        clone.ShowSeconds = ShowSeconds;
        return clone;
    }
}

public class DiskWidgetSettings : BaseWidgetSettings
{
    public override BaseWidgetSettings Clone()
    {
        var clone = (DiskWidgetSettings)base.Clone();
        return clone;
    }
}

public class FolderViewWidgetSettings : BaseWidgetSettings
{
    public string FolderPath { get; set; } = $"C:\\";

    public bool ShowIconOverlay { get; set; } = true;

    public bool ShowHiddenFile { get; set; } = false;

    public bool AllowNavigation { get; set; } = true;

    public bool ShowExtension { get; set; } = false;

    public override BaseWidgetSettings Clone()
    {
        var clone = (FolderViewWidgetSettings)base.Clone();
        clone.FolderPath = FolderPath;
        clone.ShowIconOverlay = ShowIconOverlay;
        clone.ShowHiddenFile = ShowHiddenFile;
        clone.AllowNavigation = AllowNavigation;
        clone.ShowExtension = ShowExtension;
        return clone;
    }
}

public class NetworkWidgetSettings : BaseWidgetSettings
{
    public bool ShowBps { get; set; } = false;

    public override BaseWidgetSettings Clone()
    {
        var clone = (NetworkWidgetSettings)base.Clone();
        clone.ShowBps = ShowBps;
        return clone;
    }
}

public class PerformanceWidgetSettings : BaseWidgetSettings
{
    public override BaseWidgetSettings Clone()
    {
        var clone = (PerformanceWidgetSettings)base.Clone();
        return clone;
    }
}