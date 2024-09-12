using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Services;

internal class PublicAPIService : IPublicAPIService
{
    private static IThemeSelectorService ThemeSelectorService => DependencyExtensions.GetRequiredService<IThemeSelectorService>();
    private static IWidgetManagerService WidgetManagerService => DependencyExtensions.GetRequiredService<IWidgetManagerService>();

    public ElementTheme RootTheme => ThemeSelectorService.Theme;

    public Action<ElementTheme>? ElementTheme_Changed { get; set; }

    public async Task UpdateWidgetSettings(FrameworkElement element, BaseWidgetSettings settings)
    {
        var widgetId = WidgetProperties.GetId(element);
        var indexTag = WidgetProperties.GetIndexTag(element);
        await WidgetManagerService.UpdateWidgetSettingsAsync(widgetId, indexTag, settings);
    }

    public async Task UpdateWidgetSettings(BaseWidgetViewModel viewModel, BaseWidgetSettings settings)
    {
        if (viewModel.WidgetWindow is WidgetWindow widgetWindow)
        {
            await WidgetManagerService.UpdateWidgetSettingsAsync(widgetWindow.Id, widgetWindow.IndexTag, settings);
        }
    }
}
