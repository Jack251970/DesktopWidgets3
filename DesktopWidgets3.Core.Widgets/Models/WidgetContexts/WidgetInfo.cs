namespace DesktopWidgets3.Core.Widgets.Models.WidgetContexts;

public class WidgetInfo(IWidgetManagerService widgetManagerService) : IWidgetInfo
{
    public required IWidgetContext WidgetContext { get; set; }

    private readonly IWidgetManagerService _widgetManagerService = widgetManagerService;

    public BaseWidgetSettings Settings
    {
        get
        {
            var (widgetId, widgetType, widgetIndex) = _widgetManagerService.GetWidgetInfo(WidgetContext.Id);
            return _widgetManagerService.GetWidgetSettings(widgetId, widgetType, widgetIndex)!;
        }
    }
}
