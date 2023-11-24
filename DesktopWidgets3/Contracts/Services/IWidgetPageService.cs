using DesktopWidgets3.Models;

namespace DesktopWidgets3.Contracts.Services;

public interface IWidgetPageService
{
    Type GetPageType(WidgetType widgetType);
}
