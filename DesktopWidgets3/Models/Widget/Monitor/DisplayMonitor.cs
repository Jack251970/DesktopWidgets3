namespace DesktopWidgets3.Models.Widget;

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
}
