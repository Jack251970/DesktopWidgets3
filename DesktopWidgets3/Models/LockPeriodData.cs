namespace DesktopWidgets3.Models;

public class LockPeriodData
{
    public required int ID
    {
        get; set;
    }

    public required int Version
    {
        get; set;
    }

    public required DateTime StartTime
    {
        get; set;
    }

    public required DateTime EndTime
    {
        get; set;
    }

    public int? TimePeriod
    {
        get; set;
    }
}
