using DesktopWidgets3.Models.Widget;
using DesktopWidgets3.Views.Windows;

namespace DesktopWidgets3.Contracts.Services;

public interface IWidgetManagerService
{
    Task SetThemeAsync();

    Task InitializeWidgets();

    Task EnableWidget(WidgetType widgetType, int? indexTag);

    Task DisableWidget(WidgetType widgetType, int indexTag);

    Task DeleteWidget(WidgetType widgetType, int indexTag);

    void CloseAllWidgets();

    WidgetWindow GetCurrentWidgetWindow();

    DashboardWidgetItem GetCurrentEnabledDashboardWidgetItem();

    List<DashboardWidgetItem> GetAllWidgetItems();

    Task<List<DashboardWidgetItem>> GetYourWidgetItemsAsync();

    void EnterEditMode();

    void ExitEditModeAndSave();

    void ExitEditModeAndCancel();

    Task<BaseWidgetSettings?> GetWidgetSettings(WidgetType widgetType, int indexTag);

    Task UpdateWidgetSettings(WidgetType widgetType, int indexTag, BaseWidgetSettings settings);
}
