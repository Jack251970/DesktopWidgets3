namespace DesktopWidgets3.Core.Widgets.Models.WidgetItems;

public class BaseWidgetGroupItem
{
    public required string Id { get; set; }
}

public class BaseWidgetItem : BaseWidgetGroupItem
{
    public required string Type { get; set; }

    public required int IndexTag { get; set; }

    protected bool _pinned;
    public bool Pinned
    {
        get => _pinned;
        set
        {
            if (_pinned != value)
            {
                _pinned = value;
            }
        }
    }

    public BaseWidgetSettings Settings { get; set; } = new();
}
