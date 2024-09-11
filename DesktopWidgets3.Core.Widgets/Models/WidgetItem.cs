using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

// TODO: Organize these classes.

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

    public JToken? SettingsJToken { get; set; }
}

public class DashboardWidgetItem : BaseWidgetItem
{
    public required string Name { get; set; }

    public required bool IsUnknown { get; set; }

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
                if (!IsUnknown)
                {
                    EnabledChangedCallback?.Invoke(this);
                }
            }
        }
    }

    public Action<DashboardWidgetItem>? EnabledChangedCallback { get; set; }
}

public class BaseWidgetStoreItem()
{
    public required string Id { get; set; }

    public required string Version { get; set; }
}

public class WidgetStoreItem : BaseWidgetStoreItem
{
    public required string Name { get; set; }

    public required string Description { get; set; }

    public required string Author { get; set; }

    public required string Website { get; set; }

    public required string IcoPath { get; set; }
}

public class JsonWidgetStoreItem : BaseWidgetStoreItem
{
    public required bool IsPreinstalled { get; set; } = false;

    public required bool IsInstalled { get; set; } = false;

    public required List<string> ResourcesFile { get; set; }
}
