using DesktopWidgets3.Models.Widget;
using DesktopWidgets3.Views.Windows;

namespace DesktopWidgets3.Contracts.Services;

public interface IWidgetManagerService
{
    Task SetThemeAsync();

    Task InitializeWidgets();

    Task EnableWidget(WidgetType widgetType, int? indexTag);

    Task DisableWidget(WidgetType widgetType, int indexTag);

    Task DisableWidget(BlankWindow widgetWindow);

    void CloseAllWidgets();

    BlankWindow GetCurrentWidgetWindow();

    DashboardWidgetItem GetDashboardWidgetItem();

    List<DashboardWidgetItem> GetAllWidgetItems();

    Task<List<DashboardWidgetItem>> GetYourWidgetItemsAsync();

    void EnterEditMode();

    void ExitEditModeAndSave();
}
