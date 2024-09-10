using Newtonsoft.Json;
using Windows.Graphics;

namespace DesktopWidgets3.Core.Widgets.Models;

public class BaseWidgetItem
{
    public required string Id { get; set; }

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

    public BaseWidgetSettings Settings { get; set; } = new BaseWidgetSettings();
}

[JsonConverter(typeof(JsonWidgetItemConverter))]
public class JsonWidgetItem : BaseWidgetItem
{
    public required PointInt32 Position { get; set; }

    public required RectSize Size { get; set; }

    public required DisplayMonitor DisplayMonitor  { get; set; }

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

    public string? IcoPath { get; set; }

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

    public Action<DashboardWidgetItem>? EnabledChangedCallback { get; set; }
}
