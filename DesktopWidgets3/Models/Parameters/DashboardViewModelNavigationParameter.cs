namespace DesktopWidgets3.Models.Parameters;

public class DashboardViewModelNavigationParameter
{
    public required WidgetProviderType ProviderType { get; set; }

    public required string Id { get; set; }

    public required string Type { get; set; }

    public required int Index { get; set; }

    public required UpdateEvent Event { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj is DashboardViewModelNavigationParameter parameter)
        {
            return ProviderType == parameter.ProviderType && Id == parameter.Id && Type == parameter.Type && Index == parameter.Index && Event == parameter.Event;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return ProviderType.GetHashCode() ^ Id.GetHashCode() ^ Type.GetHashCode() ^ Index.GetHashCode() ^ Event.GetHashCode();
    }

    public enum UpdateEvent
    {
        Add,
        Pin,
        Unpin,
        Delete
    }
}
