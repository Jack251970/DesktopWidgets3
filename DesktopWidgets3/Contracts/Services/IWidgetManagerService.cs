using DesktopWidgets3.Models;

namespace DesktopWidgets3.Contracts.Services;

public interface IWidgetManagerService
{
    void ShowWidget(WidgetType widgetType);

    void CloseWidget(WidgetType widgetType);

    void CloseAllWidgets();

    List<DashboardListItem> GetEnabledWidgets();

    List<DashboardListItem> GetDisableWidgets();

    Task SetThemeAsync();
}
