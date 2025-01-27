using Microsoft.UI.Xaml;
using WinUIEx;

namespace DesktopWidgets3.Core.Models;

public class DisplayMonitor
{
    public string Name { get; set; } = string.Empty;

    public Rect3 RectMonitor { get; set; }

    public Rect3 RectWork { get; set; }

    public bool IsPrimary { get; set; } = false;

    public static List<DisplayMonitor> GetMonitorInfo()
    {
        var monitorInfos = MonitorInfo.GetDisplayMonitors();
        return monitorInfos.Select(x => new DisplayMonitor
        {
            Name = x.Name,
            RectMonitor = new(x.RectMonitor),
            RectWork = new(x.RectWork),
            IsPrimary = x.IsPrimary
        }).ToList();
    }

    public static DisplayMonitor GetMonitorInfo(Window? window)
    {
        if (window is not null)
        {
            var monitorInfo = MonitorInfo.GetNearestDisplayMonitor(window.GetWindowHandle());
            if (monitorInfo is not null)
            {
                return new()
                {
                    Name = monitorInfo.Name,
                    RectMonitor = new(monitorInfo.RectMonitor),
                    RectWork = new(monitorInfo.RectWork),
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
            RectMonitor = new(primaryMonitorInfo.RectMonitor),
            RectWork = new(primaryMonitorInfo.RectWork),
            IsPrimary = primaryMonitorInfo.IsPrimary
        };
    }
}
