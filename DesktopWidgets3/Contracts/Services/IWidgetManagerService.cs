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

    Task DisableWidget(WidgetWindow widgetWindow);

    void CloseAllWidgets();

    WidgetWindow GetCurrentWidgetWindow();

    DashboardWidgetItem GetCurrentEnabledDashboardWidgetItem();

    List<DashboardWidgetItem> GetAllWidgetItems();

    Task<List<DashboardWidgetItem>> GetYourWidgetItemsAsync();

    void EnterEditMode();

    void ExitEditModeAndSave();

    void ExitEditModeAndCancel();
}
