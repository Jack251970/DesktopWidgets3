namespace DesktopWidgets3.Core.Widgets.Contracts.Services;

public interface IWidgetManagerService
{
    void EnableAllEnabledWidgets();

    Task RestartWidgetsAsync();

    Task CloseAllWidgetsAsync();

    Task<int> AddWidgetAsync(string widgetId, Action<string, int> action, bool updateDashboard);

    Task EnableWidgetAsync(string widgetId, int indexTag);

    Task DisableWidgetAsync(string widgetId, int indexTag);

    Task DeleteWidgetAsync(string widgetId, int indexTag, bool refresh);

    BaseWidgetViewModel? GetWidgetViewModel(WidgetWindow widgetWindow);

    void NavigateToWidgetSettingPage(string widgetId, int indexTag);

    BaseWidgetSettings? GetWidgetSettings(string widgetId, int indexTag);

    Task UpdateWidgetSettingsAsync(string widgetId, int indexTag, BaseWidgetSettings settings, bool updateWidget, bool updateWidgetSetting);

    bool InEditMode();

    void EnterEditMode();

    Task SaveAndExitEditMode();

    void CancelChangesAndExitEditMode();
}
