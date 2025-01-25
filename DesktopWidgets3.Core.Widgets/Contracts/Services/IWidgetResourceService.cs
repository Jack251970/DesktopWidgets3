using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.Widgets.Hosts;

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

    bool IsWidgetGroupUnknown(WidgetProviderType providerType, string widgetId, string widgetType);

    Task<Brush> GetWidgetGroupIconBrushAsync(WidgetProviderType providerType, string widgetId);

    Task<Brush> GetWidgetGroupIconBrushAsync(WidgetProviderDefinition widgetProviderDefinition);

    string GetWidgetName(WidgetProviderType providerType, string widgetId, string widgetType);

    string GetWidgetDescription(WidgetProviderType providerType, string widgetId, string widgetType);

    Task<Brush> GetWidgetIconBrushAsync(WidgetProviderType providerType, string widgetId, string widgetType, ElementTheme actualTheme);

    Task<Brush> GetWidgetScreenshotBrushAsync(WidgetProviderType providerType, string widgetId, string widgetType, ElementTheme actualTheme);

    Task<Brush> GetWidgetIconBrushAsync(ComSafeWidgetDefinition widgetDefinition, ElementTheme actualTheme);

    Task<Brush> GetWidgetScreenshotBrushAsync(ComSafeWidgetDefinition widgetDefinition, ElementTheme actualTheme);

    RectSize GetWidgetDefaultSize(string widgetId, string widgetType);

    RectSize GetWidgetDefaultSize(WidgetViewModel widgetViewModel);

    (RectSize MinSize, RectSize MaxSize) GetWidgetMinMaxSize(WidgetProviderType providerType, string widgetId, string widgetType);

    bool GetWidgetAllowMultiple(WidgetProviderType widgetProvider, string widgetId, string widgetType);

    Task<List<DashboardWidgetGroupItem>> GetInstalledDashboardGroupItems();

    Task<List<DashboardWidgetItem>> GetYourDashboardWidgetItemsAsync(ElementTheme actualTheme);

    Task<DashboardWidgetItem?> GetDashboardWidgetItemAsync(string widgetId, string widgetType, int widgetIndex, ElementTheme actualTheme);

    Task<DashboardWidgetItem> GetDashboardWidgetItemAsync(string widgetId, string widgetType, int widgetIndex, WidgetViewModel widgetViewModel, ElementTheme actualTheme);

    Task<List<WidgetStoreItem>> GetInstalledWidgetStoreItemsAsync();

    Task<List<WidgetStoreItem>> GetPreinstalledAvailableWidgetStoreItemsAsync();

    Task<WidgetStoreItem?> GetWidgetStoreItemAsync(IExtensionWrapper extension);

    Task InstallWidgetAsync(string widgetId);

    Task UninstallWidgetAsync(string widgetId);
}
