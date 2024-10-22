namespace DesktopWidgets3.Core.Widgets.Models.WidgetContexts;

public class WidgetSettingContext(IWidgetManagerService widgetManagerService) : IWidgetSettingContext
{
    public required string Id { get; set; }

    public required string Type { get; set; }

    private readonly IWidgetManagerService _widgetManagerService = widgetManagerService;

    public bool IsNavigated
    {
        get
        {
            var (widgetId, widgetType, _) = _widgetManagerService.GetWidgetSettingInfo(Id);
            return _widgetManagerService.GetWidgetSettingIsNavigated(widgetId, widgetType);
        }
    }
}
