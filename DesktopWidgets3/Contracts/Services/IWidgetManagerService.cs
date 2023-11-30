using DesktopWidgets3.Models.Widget;
using DesktopWidgets3.Views.Windows;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace DesktopWidgets3.Contracts.Services;

public interface IWidgetManagerService
{
    Task InitializeWidgets();

    Task ShowWidget(WidgetType widgetType);

    void AddTitleBar(UIElement titleBar);

    Task UpdateWidgetPosition(WidgetType widgetType, PointInt32 position);

    Task UpdateWidgetSize(WidgetType widgetType, WidgetSize size);

    Task CloseWidget(WidgetType widgetType);

    void CloseAllWidgets();

    WidgetType GetWidgetType();

    BlankWindow GetWidgetWindow();

    BlankWindow? GetWidgetWindow(WidgetType widgetType);

    Task SetThemeAsync();

    List<DashboardWidgetItem> GetAllWidgets(Action<DashboardWidgetItem>? EnabledChangedCallback);
}
