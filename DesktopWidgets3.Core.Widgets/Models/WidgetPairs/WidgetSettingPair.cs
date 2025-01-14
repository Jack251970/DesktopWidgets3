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

    public bool Equals(string widgetId, string widgetType)
    {
        return WidgetId == widgetId && WidgetType == widgetType;
    }
}
