namespace DesktopWidgets3.Core.Widgets.Contracts.Services;

public interface IWidgetManagerService
{
    (string widgetId, string widgetType, int widgetIndex) GetWidgetInfo(string widgetRuntimeId);

    (string widgetId, string widgetType, int widgetIndex) GetWidgetSettingInfo(string widgetSettingRuntimeId);

    WidgetInfo? GetWidgetInfo(string widgetId, string widgetType, int widgetIndex);

    WidgetContext? GetWidgetContext(string widgetId, string widgetType, int widgetIndex);

    WidgetSettingContext? GetWidgetSettingContext(string widgetId, string widgetType);

    bool GetWidgetIsActive(string widgetId, string widgetType, int widgetIndex);

    bool GetWidgetSettingIsNavigated(string widgetId, string widgetType);

    void InitializePinnedWidgets();

    Task RestartWidgetsAsync();

    Task CloseAllWidgetsAsync();

    Task AddWidgetAsync(string widgetId, string widgetType, Action<string, string, int> action, bool updateDashboard);

    Task PinWidgetAsync(string widgetId, string widgetType, int widgetIndex, bool refresh);

    Task UnpinWidgetAsync(string widgetId, string widgetType, int widgetIndex, bool refresh);

    Task DeleteWidgetAsync(string widgetId, string widgetType, int widgetIndex, bool refresh);

    WidgetWindow? GetWidgetWindow(string widgetRuntimeId);

    void NavigateToWidgetSettingPage(string widgetId, string widgetType, int widgetIndex);

    BaseWidgetSettings? GetWidgetSettings(string widgetId, string widgetType, int widgetIndex);

    Task UpdateWidgetSettingsAsync(string widgetId, string widgetType, int widgetIndex, BaseWidgetSettings settings);

    void EnterEditMode();

    Task SaveAndExitEditMode();

    void CancelChangesAndExitEditMode();

    Task CheckEditModeAsync();
}
