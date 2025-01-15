using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace DesktopWidgets3.Core.Widgets.Contracts.Services;

public interface IWidgetResourceService
{
    Task InitalizeAsync();

    Task DisposeWidgetsAsync();

    FrameworkElement CreateWidgetContent(string widgetId, WidgetContext widgetContext);

    void UnpinWidget(string widgetId, string widgetRuntimeId, BaseWidgetSettings widgetSettings);

    void DeleteWidget(string widgetId, string widgetRuntimeId, BaseWidgetSettings widgetSettings);

    void ActivateWidget(string widgetId, WidgetContext widgetContext);

    void DeactivateWidget(string widgetId, string widgetRuntimeId);

    BaseWidgetSettings GetDefaultSettings(string widgetId, string widgetType);

    FrameworkElement CreateWidgetSettingContent(string widgetId, WidgetSettingContext widgetSettingContext);

    void OnWidgetSettingsChanged(string widgetId, WidgetSettingsChangedArgs settingsChangedArgs);

    string GetWidgetName(string widgetId, string widgetType);

    string GetWidgetDescription(string widgetId, string widgetType);

    Task<Brush> GetWidgetIconBrushAsync(DispatcherQueue dispatcherQueue, string widgetId, string widgetType, ElementTheme actualTheme);

    Task<Brush> GetWidgetIconBrushAsync(DispatcherQueue dispatcherQueue, ComSafeWidgetDefinition widgetDefinition, ElementTheme actualTheme);

    Task<Brush> GetWidgetScreenshotBrushAsync(DispatcherQueue dispatcherQueue, string widgetId, string widgetType, ElementTheme actualTheme);

    Task<Brush> GetWidgetScreenshotBrushAsync(DispatcherQueue dispatcherQueue, ComSafeWidgetDefinition widgetDefinition, ElementTheme actualTheme);

    RectSize GetWidgetDefaultSize(string widgetId, string widgetType);

    (RectSize MinSize, RectSize MaxSize) GetWidgetMinMaxSize(string widgetId, string widgetType);

    bool IsWidgetSingleInstanceAndAlreadyPinned(string widgetId, string widgetType);

    bool GetWidgetIsCustomizable(string widgetId, string widgetType);

    List<DashboardWidgetGroupItem> GetInstalledDashboardGroupItems();

    List<DashboardWidgetItem> GetYourDashboardWidgetItems();

    DashboardWidgetItem? GetDashboardWidgetItem(string widgetId, string widgetType, int widgetIndex);

    DashboardWidgetItem? GetDashboardWidgetItem(WidgetViewModel widgetViewModel);

    bool IsWidgetGroupUnknown(string widgetId, string widgetType);

    List<WidgetStoreItem> GetInstalledWidgetStoreItems();

    List<WidgetStoreItem> GetPreinstalledAvailableWidgetStoreItems();

    Task InstallWidgetAsync(string widgetId);

    Task UninstallWidgetAsync(string widgetId);
}
