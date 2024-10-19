using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Windows.Graphics;

namespace DesktopWidgets3.Core.Widgets.Models;

public class BaseWidgetGroupItem
{
    public required string Id { get; set; }
}

public class BaseWidgetItem : BaseWidgetGroupItem
{
    public required string Type { get; set; }

    public required int IndexTag { get; set; }

    protected bool _pinned;
    public bool Pinned
    {
        get => _pinned;
        set
        {
            if (_pinned != value)
            {
                _pinned = value;
            }
        }
    }

    public BaseWidgetSettings Settings { get; set; } = new BaseWidgetSettings();
}

[JsonConverter(typeof(JsonWidgetItemConverter))]
public class JsonWidgetItem : BaseWidgetItem
{
    public required string Name { get; set; }

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

public class DashboardWidgetGroupItem : BaseWidgetGroupItem
{
    public required string Name { get; set; }

    public required string IcoPath { get; set; }

    public required List<string> Types { get; set; }
}

public class DashboardWidgetItem : BaseWidgetItem
{
    public required string Name { get; set; }

    public required string IcoPath { get; set; }

    public new bool Pinned
    {
        get => _pinned;
        set
        {
            if (_pinned != value)
            {
                _pinned = value;
                if (Editable)
                {
                    PinnedChangedCallback?.Invoke(this);
                }
            }
        }
    }

    public required bool IsUnknown { get; set; }

    public required bool IsInstalled { get; set; }

    public bool Editable => (!IsUnknown) && IsInstalled;

    public Action<DashboardWidgetItem>? PinnedChangedCallback { get; set; }
}

public class BaseWidgetStoreItem : BaseWidgetGroupItem
{
    public required string Version { get; set; }
}

public class JsonWidgetStoreItem : BaseWidgetStoreItem
{
    public required bool IsPreinstalled { get; set; }

    public required bool IsInstalled { get; set; }

    public required string ResourcesFolder { get; set; }
}

public class WidgetStoreItem : BaseWidgetStoreItem
{
    public required string Name { get; set; }

    public required string Description { get; set; }

    public required string Author { get; set; }

    public required string Website { get; set; }

    public required string IcoPath { get; set; }
}
