namespace DesktopWidgets3.Core.Widgets.Models.WidgetContexts;

public class WidgetContext(IWidgetManagerService widgetManagerService) : IWidgetContext
{
    public required string Id { get; set; }

    public required string Type { get; set; }

    private readonly IWidgetManagerService _widgetManagerService = widgetManagerService;

    public bool IsActive
    {
        get
        {
            var (widgetId, widgetType, widgetIndex) = _widgetManagerService.GetWidgetInfo(Id);
            return _widgetManagerService.GetWidgetIsActive(widgetId, widgetType, widgetIndex);
        }
    }
}
