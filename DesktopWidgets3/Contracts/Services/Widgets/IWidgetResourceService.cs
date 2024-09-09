using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Contracts.Services.Widgets;

public interface IWidgetResourceService
{
    void Initalize();

    List<DashboardWidgetItem> GetAllWidgetItems();

    Task<List<DashboardWidgetItem>> GetYourWidgetItemsAsync();

    FrameworkElement GetWidgetFrameworkElement(string widgetId);

    string GetWidgetLabel(string widgetId);

    string GetWidgetIconSource(string widgetId);

    RectSize GetDefaultSize(string widgetId);

    RectSize GetMinSize(string widgetId);

    BaseWidgetSettings GetDefaultSetting(string widgetId);

    bool GetWidgetInNewThread(string widgetId);
}
