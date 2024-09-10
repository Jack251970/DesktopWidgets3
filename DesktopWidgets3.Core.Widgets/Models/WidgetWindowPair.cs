using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Core.Widgets.Models;

public class WidgetWindowPair
{
    public WidgetWindow Window { get; set; } = null!;

    public MenuFlyout? Menu { get; set; }

    public BaseWidgetViewModel? ViewModel { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj is WidgetWindowPair widgetWindowPair)
        {
            return Window.Id == widgetWindowPair.Window.Id
                && Window.IndexTag == widgetWindowPair.Window.IndexTag;
        }
        else
        {
            return false;
        }
    }

    public override int GetHashCode()
    {
        return Window.Id.GetHashCode() ^ Window.IndexTag.GetHashCode();
    }

    public override string ToString()
    {
        return $"{Window.Id} - {Window.IndexTag}";
    }
}
