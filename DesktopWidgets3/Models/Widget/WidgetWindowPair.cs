using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Models.Widget;

public class WidgetWindowPair
{
    public WidgetWindow Window { get; internal set; } = null!;

    public MenuFlyout? Menu { get; internal set; }

    public BaseWidgetViewModel? ViewModel { get; internal set; }

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