﻿using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.Models.Widget;

namespace DesktopWidgets3.Services;

public class WidgetResourceService : IWidgetResourceService
{
    public string GetWidgetLabel(WidgetType widgetType)
    {
        return widgetType switch
        {
            _ => $"Widget_{widgetType}_Label".GetLocalized(),
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
}
