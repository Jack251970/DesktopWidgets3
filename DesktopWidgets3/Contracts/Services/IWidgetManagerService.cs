using DesktopWidgets3.Models.Widget;
using Windows.Graphics;

namespace DesktopWidgets3.Contracts.Services;

public interface IWidgetManagerService
{
    Task InitializeWidgets();

    Task ShowWidget(WidgetType widgetType);

    Task UpdateWidgetPosition(WidgetType widgetType, PointInt32 position);

    Task UpdateWidgetSize(WidgetType widgetType, WidgetSize size);

    Task CloseWidget(WidgetType widgetType);

    void CloseAllWidgets();

    Task SetThemeAsync();

    List<DashboardWidgetItem> GetAllWidgets(Action<DashboardWidgetItem>? EnabledChangedCallback);
}
