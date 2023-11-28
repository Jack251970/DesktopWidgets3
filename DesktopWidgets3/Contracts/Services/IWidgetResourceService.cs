using DesktopWidgets3.Models;
using Windows.Foundation;

namespace DesktopWidgets3.Contracts.Services;

public interface IWidgetResourceService
{
    public Size GetDefaultSize(WidgetType widgetType);
}
