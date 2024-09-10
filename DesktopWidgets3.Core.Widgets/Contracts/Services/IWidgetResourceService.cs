using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Core.Widgets.Contracts.Services;

public interface IWidgetResourceService
{
    Task Initalize();

    List<DashboardWidgetItem> GetAllDashboardItems();

    Task<List<DashboardWidgetItem>> GetYourDashboardItemsAsync();

    DashboardWidgetItem GetDashboardItem(string widgetId, int indexTag);

    FrameworkElement GetWidgetFrameworkElement(string widgetId);

    RectSize GetDefaultSize(string widgetId);

    RectSize GetMinSize(string widgetId);

    BaseWidgetSettings GetDefaultSetting(string widgetId);

    FrameworkElement GetWidgetSettingFrameworkElement(string widgetId);

    bool GetWidgetInNewThread(string widgetId);
}
