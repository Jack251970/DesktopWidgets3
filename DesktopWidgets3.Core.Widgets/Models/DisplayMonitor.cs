﻿namespace DesktopWidgets3.Core.Widgets.Models;

public class DisplayMonitor
{
    public string Name { get; internal set; } = string.Empty;

    public RectSize RectMonitor { get; internal set; }

    public RectSize RectWork { get; internal set; }

    public bool IsPrimary { get; internal set; }

    public static DisplayMonitor GetMonitorInfo(WindowEx? window)
    {
        if (window is not null)
        {
            var monitorInfo = MonitorInfo.GetNearestDisplayMonitor(window.GetWindowHandle());
            if (monitorInfo is not null)
            {
                return new()
                {
                    Name = monitorInfo.Name,
                    RectMonitor = new RectSize(monitorInfo.RectMonitor),
                    RectWork = new RectSize(monitorInfo.RectWork),
                    IsPrimary = monitorInfo.IsPrimary
                };
            }
        }
        return GetPrimaryMonitorInfo();
    }

    public static DisplayMonitor GetPrimaryMonitorInfo()
    {
        var primaryMonitorInfo = MonitorInfo.GetDisplayMonitors().FirstOrDefault(x => x.IsPrimary);
        return new()
        {
            Name = primaryMonitorInfo!.Name,
            RectMonitor = new RectSize(primaryMonitorInfo.RectMonitor),
            RectWork = new RectSize(primaryMonitorInfo.RectWork),
            IsPrimary = primaryMonitorInfo.IsPrimary
        };
    }
}
