namespace DesktopWidgets3.Services;

internal class PublicAPIService(IWidgetManagerService widgetManagerService) : IPublicAPIService
{
    private readonly IWidgetManagerService _widgetManagerService = widgetManagerService;
}
