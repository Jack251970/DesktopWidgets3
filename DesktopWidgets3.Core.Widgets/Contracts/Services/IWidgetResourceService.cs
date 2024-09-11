using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Core.Widgets.Contracts.Services;

public interface IWidgetResourceService
{
    Task InitalizeAsync();

    Task DisposeWidgetsAsync();

    FrameworkElement GetWidgetFrameworkElement(string widgetId);

    Task EnvokeEnableWidgetAsync(string widgetId, bool firstWidget);

    Task EnvokeDisableWidgetAsync(string widgetId, bool lastWidget);

    BaseWidgetSettings GetDefaultSetting(string widgetId);

    FrameworkElement GetWidgetSettingFrameworkElement(string widgetId);

    RectSize GetDefaultSize(string widgetId);

    RectSize GetMinSize(string widgetId);

    bool GetWidgetInNewThread(string widgetId);

    List<DashboardWidgetItem> GetAllDashboardItems();

    Task<List<DashboardWidgetItem>> GetYourDashboardItemsAsync();

    DashboardWidgetItem GetDashboardItem(string widgetId, int indexTag);

    bool IsWidgetUnknown(string widgetId);

    List<WidgetStoreItem> GetInstalledWidgetStoreItems();

    List<WidgetStoreItem> GetPreinstalledAvailableWidgetStoreItems();

    Task<bool> InstallWidget(string widgetId);

    Task UninstallWidget(string widgetId);
}
