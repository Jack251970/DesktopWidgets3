using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Services.Widgets;

internal class WidgetService : IWidgetService
{
    private static IWidgetManagerService WidgetManagerService => DependencyExtensions.GetRequiredService<IWidgetManagerService>();

    public async Task UpdateWidgetSettings(FrameworkElement element, BaseWidgetSettings settings, bool updateWidget, bool updateWidgetSetting)
    {
        var widgetId = WidgetProperties.GetId(element);
        var indexTag = WidgetProperties.GetIndexTag(element);
        await WidgetManagerService.UpdateWidgetSettingsAsync(widgetId, indexTag, settings, updateWidget, updateWidgetSetting);
    }

    public async Task UpdateWidgetSettings(BaseWidgetViewModel viewModel, BaseWidgetSettings settings, bool updateWidget, bool updateWidgetSetting)
    {
        var widgetId = viewModel.Id;
        var indexTag = viewModel.IndexTag;
        await WidgetManagerService.UpdateWidgetSettingsAsync(widgetId, indexTag, settings, updateWidget, updateWidgetSetting);
    }
}
