using DesktopWidgets3.Models;
using Windows.Foundation;
using Windows.Graphics;

namespace DesktopWidgets3.Contracts.Services;

public interface IWidgetManagerService
{
    void InitializeWidgets();

    void ShowWidget(WidgetType widgetType);

    void UpdateWidgetPosition(WidgetType widgetType, PointInt32 position);

    void UpdateWidgetSize(WidgetType widgetType, Size size);

    void SetEditMode(bool isEditMode);

    void CloseWidget(WidgetType widgetType);

    void CloseAllWidgets();

    Task SetThemeAsync();

    List<DashboardWidgetItem> GetAllWidgets(Action<DashboardWidgetItem>? EnabledChangedCallback);
}
