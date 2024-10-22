namespace DesktopWidgets3.Services.Widgets;

internal class WidgetService : IWidgetService
{
    private readonly IWidgetManagerService _widgetManagerService = DependencyExtensions.GetRequiredService<IWidgetManagerService>();

    public async Task UpdateWidgetSettingsAsync(string widgetRuntimeId, BaseWidgetSettings settings)
    {
        // get widget info
        var (widgetId, widgetType, widgetIndex) = _widgetManagerService.GetWidgetInfo(widgetRuntimeId);

        // update widget settings
        await _widgetManagerService.UpdateWidgetSettingsAsync(widgetId, widgetType, widgetIndex, settings);
    }
}
