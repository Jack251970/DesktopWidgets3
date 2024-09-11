namespace DesktopWidgets3.Core.Widgets.Contracts.Services;

public interface IWidgetManagerService
{
    Task Initialize();

    Task<int> AddWidget(string widgetId, bool refresh);

    Task EnableWidget(string widgetId, int indexTag);

    Task DisableWidget(string widgetId, int indexTag);

    Task DeleteWidget(string widgetId, int indexTag, bool refresh);

    Task DisableAllWidgets();

    bool IsWidgetEnabled(string widgetId, int indexTag);

    BaseWidgetViewModel? GetWidgetViewModel(WidgetWindow widgetWindow);

    Task NavigateToWidgetSettingPage(string widgetId, int indexTag);

    void EnterEditMode();

    void SaveAndExitEditMode();

    void CancelAndExitEditMode();

    Task<BaseWidgetSettings?> GetWidgetSettings(string widgetId, int indexTag);

    Task UpdateWidgetSettings(string widgetId, int indexTag, BaseWidgetSettings settings);
}
