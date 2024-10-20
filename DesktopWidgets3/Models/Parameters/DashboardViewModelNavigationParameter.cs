namespace DesktopWidgets3.Models.Parameters;

public class DashboardViewModelNavigationParameter
{
    public required string Id { get; set; }

    public required string Type { get; set; }

    public required int Index { get; set; }

    public required UpdateEvent Event { get; set; }

    public enum UpdateEvent
    {
        Pin,
        Unpin,
        Delete
    }
}
