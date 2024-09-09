using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Contracts.Services.Widgets;

public interface IWidgetResourceService
{
    void Initalize();

    List<DashboardWidgetItem> GetAllDashboardItems();

    Task<List<DashboardWidgetItem>> GetYourDashboardItemsAsync();

    DashboardWidgetItem GetDashboardItem(string widgetId, int indexTag);

    FrameworkElement GetWidgetFrameworkElement(string widgetId);

    RectSize GetDefaultSize(string widgetId);

    RectSize GetMinSize(string widgetId);

    BaseWidgetSettings GetDefaultSetting(string widgetId);

    bool GetWidgetInNewThread(string widgetId);
}
