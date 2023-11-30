using DesktopWidgets3.Models.Widget;
using DesktopWidgets3.Views.Windows;
using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Contracts.Services;

public interface IWidgetManagerService
{
    Task SetThemeAsync();

    Task InitializeWidgets();

    Task ShowWidget(WidgetType widgetType, int? indexTag);

    void AddCurrentTitleBar(UIElement titleBar);

    Task UpdateAllWidgets();

    Task CloseWidget(WidgetType widgetType, int indexTag);

    void CloseAllWidgets();

    WidgetType GetWidgetType();

    BlankWindow GetWidgetWindow();

    List<DashboardWidgetItem> GetAllWidgetItems();

    Task<List<DashboardWidgetItem>> GetYourWidgetItemsAsync();
}
