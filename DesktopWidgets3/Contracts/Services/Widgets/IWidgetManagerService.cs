namespace DesktopWidgets3.Contracts.Services.Widgets;

public interface IWidgetManagerService
{
    Task Initialize();

    Task AddWidget(string widgetId);

    Task EnableWidget(string widgetId, int indexTag);

    Task DisableWidget(string widgetId, int indexTag);

    Task DeleteWidget(string widgetId, int indexTag);

    Task DisableAllWidgets();

    bool IsWidgetEnabled(string widgetId, int indexTag);

    void EnterEditMode();

    void SaveAndExitEditMode();

    void CancelAndExitEditMode();

    Task<BaseWidgetSettings?> GetWidgetSettings(string widgetId, int indexTag);

    Task UpdateWidgetSettings(string widgetId, int indexTag, BaseWidgetSettings settings);
}
