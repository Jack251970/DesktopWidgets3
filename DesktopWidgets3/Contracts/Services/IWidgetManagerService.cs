using DesktopWidgets3.Models.Widget;
using DesktopWidgets3.Views.Windows;

namespace DesktopWidgets3.Contracts.Services;

public interface IWidgetManagerService
{
    Task SetThemeAsync();

    Task InitializeWidgets();

    Task ShowWidget(WidgetType widgetType, int? indexTag);

    void InitializeDragZone();

    Task UpdateAllWidgets();

    Task CloseWidget(WidgetType widgetType, int indexTag);

    void CloseAllWidgets();

    BlankWindow GetCurrentWidgetWindow();

    DashboardWidgetItem GetDashboardWidgetItem();

    List<DashboardWidgetItem> GetAllWidgetItems();

    Task<List<DashboardWidgetItem>> GetYourWidgetItemsAsync();
}
