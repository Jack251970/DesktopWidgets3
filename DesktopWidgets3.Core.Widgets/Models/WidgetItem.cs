﻿using Newtonsoft.Json;
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

public class DashboardWidgetItem : BaseWidgetItem
{
    public required string Name { get; set; }

    public required string IcoPath { get; set; }

    public new bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled != value)
            {
                _isEnabled = value;
                if (Editable)
                {
                    EnabledChangedCallback?.Invoke(this);
                }
            }
        }
    }

    public required bool IsUnknown { get; set; }

    public required bool IsInstalled { get; set; }

    public bool Editable => (!IsUnknown) && IsInstalled;

    public Action<DashboardWidgetItem>? EnabledChangedCallback { get; set; }
}

public class BaseWidgetStoreItem()
{
    public required string Id { get; set; }

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
