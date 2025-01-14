namespace DesktopWidgets3.Core.Widgets.Models.WidgetDefinitions;

public class DesktopWidgets3WidgetDefinition(string widgetId, string widgetType, string widgetGroupName, string widgetName)
{
    public string WidgetId { get; private set; } = widgetId;

    public string WidgetType { get; private set; } = widgetType;

    public string ProviderDefinitionDisplayName { get; private set; } = widgetGroupName;

    public string DisplayTitle { get; private set; } = widgetName;
}
