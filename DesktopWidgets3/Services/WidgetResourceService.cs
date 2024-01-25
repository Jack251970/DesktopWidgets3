﻿using DesktopWidgets3.Helpers;
using DesktopWidgets3.Models.Widget;

namespace DesktopWidgets3.Services;

public class WidgetResourceService : IWidgetResourceService
{
    public string GetWidgetLabel(WidgetType widgetType)
    {
        return widgetType switch
        {
            _ => $"Widget_{widgetType}_Label".ToLocalized(),
        };
    }

    public string GetWidgetIconSource(WidgetType widgetType)
    {
        return widgetType switch
        {
            _ => $"ms-appx:///Assets/FluentIcons/{widgetType}.png"
        };
    }

    public WidgetSize GetDefaultSize(WidgetType widgetType)
    {
        return widgetType switch
        {
            WidgetType.FolderView => new WidgetSize(500, 500),
            _ => new WidgetSize(318, 200),
        };
    }

    public WidgetSize GetMinSize(WidgetType widgetType)
    {
        return widgetType switch
        {
            _ => new WidgetSize(318, 200),
        };
    }

    public BaseWidgetSettings GetDefaultSettings(WidgetType widgetType)
    {
        return widgetType switch
        {
            WidgetType.Clock => new ClockWidgetSettings()
            {
                ShowSeconds = true,
            },
            WidgetType.Performance => new PerformanceWidgetSettings()
            {

            },
            WidgetType.Disk => new DiskWidgetSettings()
            {

            },
            WidgetType.FolderView => new FolderViewWidgetSettings()
            {
                FolderPath = "C:\\",
                ShowIconOverlay = true,
                ShowHiddenFile = false,
                AllowNavigation = true,
            },
            WidgetType.Network => new NetworkWidgetSettings()
            {
                ShowBps = false,
            },
            _ => throw new ArgumentOutOfRangeException(nameof(widgetType), widgetType, null),
        };
    }
}
