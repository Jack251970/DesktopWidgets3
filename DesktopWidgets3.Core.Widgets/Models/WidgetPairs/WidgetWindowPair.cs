using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Core.Widgets.Models.WidgetPairs;

public class WidgetWindowPair
{
    public string RuntimeId { get; set; } = string.Empty;

    public WidgetInfo WidgetInfo { get; set; } = null!;

    public WidgetWindow Window { get; set; } = null!;

    public MenuFlyout MenuFlyout { get; set; } = null!;

    public override bool Equals(object? obj)
    {
        if (obj is WidgetWindowPair widgetWindowPair)
        {
            return RuntimeId == widgetWindowPair.RuntimeId;
        }
        else
        {
            return false;
        }
    }

    public override int GetHashCode()
    {
        return RuntimeId.GetHashCode();
    }

    public override string ToString()
    {
        return RuntimeId;
    }
}
