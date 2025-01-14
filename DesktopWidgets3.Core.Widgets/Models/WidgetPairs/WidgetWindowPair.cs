using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Core.Widgets.Models.WidgetPairs;

public class WidgetWindowPair
{
    public required string RuntimeId { get; set; }

    public required WidgetProviderType ProviderType { get; set; }

    public required string WidgetId { get; set; }

    public required string WidgetType { get; set; }

    public required int WidgetIndex { get; set; }

    public required WidgetInfo WidgetInfo { get; set; }

    public WidgetWindow Window { get; set; } = null!;

    public MenuFlyout MenuFlyout { get; set; } = null!;

    public bool Equals(WidgetProviderType providerType, string widgetId, string widgetType)
    {
        return ProviderType == providerType && WidgetId == widgetId && WidgetType == widgetType;
    }

    public bool Equals(WidgetProviderType providerType, string widgetId, string widgetType, int widgetIndex)
    {
        return ProviderType == providerType && WidgetId == widgetId && WidgetType == widgetType && WidgetIndex == widgetIndex;
    }
}
