using Microsoft.UI.Dispatching;

namespace DesktopWidgets3.Services.Widgets;

internal class WidgetService : IWidgetService
{
    private readonly IWidgetManagerService _widgetManagerService = DependencyExtensions.GetRequiredService<IWidgetManagerService>();

    public DispatcherQueue GetDispatcherQueue(string widgetRuntimeId)
    {
        var widgetWindow = _widgetManagerService.GetWidgetWindow(widgetRuntimeId);
        if (widgetWindow != null)
        {
            // from widget
            return widgetWindow.DispatcherQueue;
        }
        // from widget setting
        return App.MainWindow.DispatcherQueue;
    }

    public async Task UpdateWidgetSettingsAsync(string widgetRuntimeId, BaseWidgetSettings settings)
    {
        // get widget info
        var (widgetId, widgetType, widgetIndex) = _widgetManagerService.GetWidgetInfo(widgetRuntimeId);
        if (widgetId != string.Empty)
        {
            // update widget settings
            await _widgetManagerService.UpdateWidgetSettingsAsync(widgetId, widgetType, widgetIndex, settings);
        }
        else
        {
            // get widget setting info
            (widgetId, widgetType, widgetIndex) = _widgetManagerService.GetWidgetSettingInfo(widgetRuntimeId);
            if (widgetId != string.Empty)
            {
                // update widget settings
                await _widgetManagerService.UpdateWidgetSettingsAsync(widgetId, widgetType, widgetIndex, settings);
            }
        }
    }
}
