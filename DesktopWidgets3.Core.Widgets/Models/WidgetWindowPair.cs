using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Core.Widgets.Models;

public class WidgetWindowPair
{
    public WidgetWindow Window { get; set; } = null!;

    public BaseWidgetViewModel? ViewModel { get; set; }

    public MenuFlyout MenuFlyout { get; set; } = null!;

    public override bool Equals(object? obj)
    {
        if (obj is WidgetWindowPair widgetWindowPair)
        {
            return Window.Id == widgetWindowPair.Window.Id
                && Window.Index == widgetWindowPair.Window.Index;
        }
        else
        {
            return false;
        }
    }

    public override int GetHashCode()
    {
        return Window.Id.GetHashCode() ^ Window.Index.GetHashCode();
    }

    public override string ToString()
    {
        return $"{Window.Id} - {Window.Index}";
    }
}
