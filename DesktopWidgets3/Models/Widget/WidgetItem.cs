using Newtonsoft.Json;
using Windows.Graphics;

namespace DesktopWidgets3.Models.Widget;

public class BaseWidgetItem
{
    public required WidgetType Type { get; set; }

    public required int IndexTag { get; set; }

    protected bool _isEnabled;
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled != value)
            {
                _isEnabled = value;
            }
        }
    }

    protected BaseWidgetSettings widgetSettings = null!;
    public BaseWidgetSettings Settings
    {
        get => Type switch
        {
            WidgetType.Clock => (ClockWidgetSettings)widgetSettings,
            WidgetType.Performance => (PerformanceWidgetSettings)widgetSettings,
            WidgetType.Disk => (DiskWidgetSettings)widgetSettings,
            WidgetType.FolderView => (FolderViewWidgetSettings)widgetSettings,
            WidgetType.Network => (NetworkWidgetSettings)widgetSettings,
            _ => throw new ArgumentOutOfRangeException(),
        };
        set => widgetSettings = value;
    }
}

[JsonConverter(typeof(JsonWidgetItemConverter))]
public class JsonWidgetItem : BaseWidgetItem
{
    public required PointInt32 Position { get; set; }

    public required RectSize Size { get; set; }

    public new required BaseWidgetSettings Settings
    {
        get => base.Settings;
        set => base.Settings = value;
    }
}

public class DashboardWidgetItem : BaseWidgetItem
{
    public required string Label { get; set; }

    public string? Description { get; set; }

    public string? Icon { get; set; }

    public new bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled != value)
            {
                _isEnabled = value;
                EnabledChangedCallback?.Invoke(this);
            }
        }
    }

    public Action<DashboardWidgetItem>? EnabledChangedCallback
    {
        get; set;
    }
}