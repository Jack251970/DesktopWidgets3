namespace DesktopWidgets3.Contracts.Services.Widgets;

public interface IWidgetManagerService
{
    Task EnableAllEnabledWidgets();

    Task AddWidget(WidgetType widgetType);

    Task EnableWidget(WidgetType widgetType, int indexTag);

    Task DisableWidget(WidgetType widgetType, int indexTag);

    Task DeleteWidget(WidgetType widgetType, int indexTag);

    void DisableAllWidgets();

    WidgetWindow GetLastWidgetWindow();

    bool IsWidgetEnabled(WidgetType widgetType, int indexTag);

    DashboardWidgetItem GetCurrentEnabledWidget();

    List<DashboardWidgetItem> GetAllWidgetItems();

    Task<List<DashboardWidgetItem>> GetYourWidgetItemsAsync();

    void EnterEditMode();

    void SaveAndExitEditMode();

    void CancelAndExitEditMode();

    Task<BaseWidgetSettings?> GetWidgetSettings(WidgetType widgetType, int indexTag);

    Task UpdateWidgetSettings(WidgetType widgetType, int indexTag, BaseWidgetSettings settings);

    void WidgetNavigateTo(WidgetType widgetType, int indexTag, object? parameter = null, bool clearNavigation = false);
}
