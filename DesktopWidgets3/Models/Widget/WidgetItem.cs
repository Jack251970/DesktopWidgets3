using Newtonsoft.Json;
using Windows.Graphics;

namespace DesktopWidgets3.Models.Widget;

public class BaseWidgetItem
{
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

    public WidgetType Type { get; set; }

    public int IndexTag { get; set; }

    protected BaseWidgetSettings widgetSettings = null!;
    public BaseWidgetSettings Settings
    {
        get {
            if (widgetSettings is null)
            {
                throw new NullReferenceException();
            }
            return Type switch
            {
                WidgetType.Clock => (ClockWidgetSettings)widgetSettings,
                WidgetType.CPU => (CPUWidgetSettings)widgetSettings,
                WidgetType.Disk => (DiskWidgetSettings)widgetSettings,
                WidgetType.FolderView => (FolderViewWidgetSettings)widgetSettings,
                WidgetType.Network => (NetworkWidgetSettings)widgetSettings,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }
        set => widgetSettings = value;
    }
}

[JsonConverter(typeof(JsonWidgetItemConverter))]
public class JsonWidgetItem : BaseWidgetItem
{
    public PointInt32 Position { get; set; }

    public WidgetSize Size { get; set; }
}

public class DashboardWidgetItem : BaseWidgetItem
{
    public string? Label { get; set; }

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