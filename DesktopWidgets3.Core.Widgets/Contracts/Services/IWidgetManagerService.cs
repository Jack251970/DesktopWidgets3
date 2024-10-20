namespace DesktopWidgets3.Core.Widgets.Contracts.Services;

public interface IWidgetManagerService
{
    void InitializePinnedWidgets();

    Task RestartWidgetsAsync();

    Task CloseAllWidgetsAsync();

    Task<int> AddWidgetAsync(string widgetId, string widgetType, Action<string, string, int> action, bool updateDashboard);

    Task PinWidgetAsync(string widgetId, string widgetType, int indexTag);

    Task UnpinWidgetAsync(string widgetId, string widgetType, int indexTag, bool refresh);

    Task DeleteWidgetAsync(string widgetId, string widgetType, int indexTag, bool refresh);

    BaseWidgetViewModel? GetWidgetViewModel(WidgetWindow widgetWindow);

    void NavigateToWidgetSettingPage(string widgetId, string widgetType, int indexTag);

    BaseWidgetSettings? GetWidgetSettings(string widgetId, string widgetType, int indexTag);

    Task UpdateWidgetSettingsAsync(string widgetId, string widgetType, int indexTag, BaseWidgetSettings settings, bool updateWidget, bool updateWidgetSetting);

    void EnterEditMode();

    Task SaveAndExitEditMode();

    void CancelChangesAndExitEditMode();

    Task CheckEditModeAsync();
}
