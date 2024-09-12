namespace DesktopWidgets3.Core.Widgets.Models;

public class DisplayMonitor
{
    public string Name { get; set; } = string.Empty;

    public RectSize RectMonitor { get; set; } = new(null, null);

    public RectSize RectWork { get; set; } = new(null, null);

    public bool IsPrimary { get; set; } = false;

    public static List<DisplayMonitor> GetMonitorInfo()
    {
        var monitorInfos = MonitorInfo.GetDisplayMonitors();
        return monitorInfos.Select(x => new DisplayMonitor
        {
            Name = x.Name,
            RectMonitor = new RectSize(x.RectMonitor),
            RectWork = new RectSize(x.RectWork),
            IsPrimary = x.IsPrimary
        }).ToList();
    }

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
