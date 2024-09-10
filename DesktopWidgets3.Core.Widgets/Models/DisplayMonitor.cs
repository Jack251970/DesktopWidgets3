namespace DesktopWidgets3.Core.Widgets.Models;

public class DisplayMonitor
{
    public string Name { get; set; } = string.Empty;

    public RectSize RectMonitor { get; set; } = new();

    public RectSize RectWork { get; set; } = new();

    public bool IsPrimary { get; set; } = false;

    public DisplayMonitor()
    {
    }

    public DisplayMonitor(MonitorInfo displayInfo)
    {
        Name = displayInfo.Name;
        RectMonitor = new(displayInfo.RectMonitor);
        RectWork = new(displayInfo.RectWork);
        // TODO: Add IsPrimary info.
        IsPrimary = true;
    }

    public static MonitorInfo GetMonitorInfo(WindowEx? window)
    {
        return MonitorInfo.GetDisplayMonitors().First();
        // TODO: get monitor info here.
        /*if (window is null)
        {
            var primaryMonitorInfo = MonitorInfo.GetDisplayMonitors().First();
            return primaryMonitorInfo;
        }
        else
        {
            
        }*/
    }
}
