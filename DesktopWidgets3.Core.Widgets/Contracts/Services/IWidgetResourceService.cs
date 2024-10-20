using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Core.Widgets.Contracts.Services;

public interface IWidgetResourceService
{
    Task InitalizeAsync();

    Task DisposeWidgetsAsync();

    FrameworkElement GetWidgetContent(string widgetId, string widgetType);

    BaseWidgetSettings GetDefaultSettings(string widgetId, string widgetType);

    FrameworkElement GetWidgetSettingContent(string widgetId, string widgetType);

    string GetWidgetName(string widgetId, string widgetType);

    string GetWidgetDescription(string widgetId, string widgetType);

    string GetWidgetIconPath(string widgetId, string widgetType, ElementTheme? actualTheme = null);

    string GetWidgetScreenshotPath(string widgetId, string widgetType, ElementTheme? actualTheme = null);

    RectSize GetWidgetDefaultSize(string widgetId, string widgetType);

    (RectSize MinSize, RectSize MaxSize) GetWidgetMinMaxSize(string widgetId, string widgetType);

    bool IsWidgetSingleInstanceAndAlreadyPinned(string widgetId, string widgetType);

    bool GetWidgetIsCustomizable(string widgetId, string widgetType);

    List<DashboardWidgetGroupItem> GetInstalledDashboardGroupItems();

    List<DashboardWidgetItem> GetYourDashboardWidgetItems();

    DashboardWidgetItem? GetDashboardWidgetItem(string widgetId, string widgetType, int widgetIndex);

    bool IsWidgetGroupUnknown(string widgetId, string widgetType);

    List<WidgetStoreItem> GetInstalledWidgetStoreItems();

    List<WidgetStoreItem> GetPreinstalledAvailableWidgetStoreItems();

    Task InstallWidgetAsync(string widgetId);

    Task UninstallWidgetAsync(string widgetId);
}
