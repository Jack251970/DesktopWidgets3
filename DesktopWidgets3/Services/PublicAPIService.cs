using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Services;

internal class PublicAPIService(IWidgetManagerService widgetManagerService) : IPublicAPIService
{
    private readonly IWidgetManagerService _widgetManagerService = widgetManagerService;

    public async Task UpdateWidgetSettings(FrameworkElement element, BaseWidgetSettings settings)
    {
        var widgetId = WidgetProperties.GetId(element);
        var indexTag = WidgetProperties.GetIndexTag(element);
        await _widgetManagerService.UpdateWidgetSettings(widgetId, indexTag, settings);
    }

    public async Task UpdateWidgetSettings(BaseWidgetViewModel viewModel, BaseWidgetSettings settings)
    {
        if (viewModel.WidgetWindow is WidgetWindow widgetWindow)
        {
            await _widgetManagerService.UpdateWidgetSettings(widgetWindow.Id, widgetWindow.IndexTag, settings);
        }
    }
}
