using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Services;

internal class PublicAPIService : IPublicAPIService
{
    private static IThemeSelectorService ThemeSelectorService => App.GetService<IThemeSelectorService>();
    private static IWidgetManagerService WidgetManagerService => App.GetService<IWidgetManagerService>();

    public ElementTheme RootTheme => ThemeSelectorService.Theme;

    public Action<ElementTheme>? ElementTheme_Changed { get; set; }

    public async Task UpdateWidgetSettings(FrameworkElement element, BaseWidgetSettings settings)
    {
        var widgetId = WidgetProperties.GetId(element);
        var indexTag = WidgetProperties.GetIndexTag(element);
        await WidgetManagerService.UpdateWidgetSettings(widgetId, indexTag, settings);
    }

    public async Task UpdateWidgetSettings(BaseWidgetViewModel viewModel, BaseWidgetSettings settings)
    {
        if (viewModel.WidgetWindow is WidgetWindow widgetWindow)
        {
            await WidgetManagerService.UpdateWidgetSettings(widgetWindow.Id, widgetWindow.IndexTag, settings);
        }
    }
}
