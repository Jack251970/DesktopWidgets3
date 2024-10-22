using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Core.Widgets.Models.WidgetPairs;

public class WidgetSettingPair
{
    public string RuntimeId { get; set; } = string.Empty;

    public int WidgetIndex { get; set; } = -1;

    public WidgetSettingContext WidgetSettingContext { get; set; } = null!;

    public FrameworkElement WidgetSettingContent { get; set; } = null!;

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
