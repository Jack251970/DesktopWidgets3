namespace DesktopWidgets3.Core.Widgets.Contracts.Services;

public interface IWidgetManagerService
{
    (WidgetProviderType providerType, string widgetId, string widgetType, int widgetIndex) GetWidgetInfo(string widgetRuntimeId);

    (string widgetId, string widgetType, int widgetIndex) GetWidgetSettingInfo(string widgetSettingRuntimeId);

    WidgetInfo? GetWidgetInfo(WidgetProviderType providerType, string widgetId, string widgetType, int widgetIndex);

    WidgetContext? GetWidgetContext(WidgetProviderType providerType, string widgetId, string widgetType, int widgetIndex);

    WidgetSettingContext? GetWidgetSettingContext(string widgetId, string widgetType);

    bool GetWidgetIsActive(WidgetProviderType providerType, string widgetId, string widgetType, int widgetIndex);

    bool GetWidgetSettingIsNavigated(string widgetId, string widgetType);

    WidgetViewModel? GetWidgetViewModel(string widgetId, string widgetType, int widgetIndex);

    void InitializePinnedWidgets();

    Task RestartWidgetsAsync();

    Task CloseAllWidgetsAsync();

    Task AddWidgetAsync(string widgetId, string widgetType, Func<string, string, int, Task>? action, bool updateDashboard);

    Task<int> AddWidgetAsync(WidgetViewModel widgetViewModel, Func<string, string, int, WidgetViewModel, Task>? action, bool updateDashboard);

    Task PinWidgetAsync(WidgetProviderType providerType, string widgetId, string widgetType, int widgetIndex, bool refresh);

    Task UnpinWidgetAsync(WidgetProviderType providerType, string widgetId, string widgetType, int widgetIndex, bool refresh);

    Task DeleteWidgetAsync(WidgetProviderType providerType, string widgetId, string widgetType, int widgetIndex, bool refresh);

    WidgetWindow? GetWidgetWindow(string widgetRuntimeId);

    void NavigateToWidgetSettingPage(string widgetId, string widgetType, int widgetIndex);

    BaseWidgetSettings? GetWidgetSettings(string widgetId, string widgetType, int widgetIndex);

    Task UpdateWidgetSettingsAsync(string widgetId, string widgetType, int widgetIndex, BaseWidgetSettings settings);

    void EnterEditMode();

    Task SaveAndExitEditMode();

    void CancelChangesAndExitEditMode();

    Task CheckEditModeAsync();
}
