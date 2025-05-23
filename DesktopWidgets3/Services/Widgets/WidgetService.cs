﻿namespace DesktopWidgets3.Services.Widgets;

internal class WidgetService(IWidgetManagerService widgetManagerService) : IWidgetService
{
    private readonly IWidgetManagerService _widgetManagerService = widgetManagerService;

    public async Task UpdateWidgetSettingsAsync(string widgetRuntimeId, BaseWidgetSettings settings)
    {
        // get widget info
        var (_, widgetId, widgetType, widgetIndex) = _widgetManagerService.GetWidgetInfo(widgetRuntimeId);
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
