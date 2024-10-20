namespace DesktopWidgets3.Core.Widgets.Contracts.Services;

public interface IWidgetManagerService
{
    void InitializePinnedWidgets();

    Task RestartWidgetsAsync();

    Task CloseAllWidgetsAsync();

    Task<int> AddWidgetAsync(string widgetId, string widgetType, Action<string, string, int> action, bool updateDashboard);

    Task PinWidgetAsync(string widgetId, string widgetType, int widgetIndex);

    Task UnpinWidgetAsync(string widgetId, string widgetType, int widgetIndex, bool refresh);

    Task DeleteWidgetAsync(string widgetId, string widgetType, int widgetIndex, bool refresh);

    BaseWidgetViewModel? GetWidgetViewModel(WidgetWindow widgetWindow);

    void NavigateToWidgetSettingPage(string widgetId, string widgetType, int widgetIndex);

    BaseWidgetSettings? GetWidgetSettings(string widgetId, string widgetType, int widgetIndex);

    Task UpdateWidgetSettingsAsync(string widgetId, string widgetType, int widgetIndex, BaseWidgetSettings settings, bool updateWidget, bool updateWidgetSetting);

    void EnterEditMode();

    Task SaveAndExitEditMode();

    void CancelChangesAndExitEditMode();

    Task CheckEditModeAsync();
}
