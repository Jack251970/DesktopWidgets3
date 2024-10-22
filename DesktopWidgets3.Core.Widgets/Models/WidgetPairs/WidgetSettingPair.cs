using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Core.Widgets.Models.WidgetPairs;

public class WidgetSettingPair
{
    public required string RuntimeId { get; set; }

    public required string WidgetId { get; set; }

    public required string WidgetType { get; set; }

    public required int WidgetIndex { get; set; }

    public required WidgetSettingContext WidgetSettingContext { get; set; }

    public required FrameworkElement WidgetSettingContent { get; set; }

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
