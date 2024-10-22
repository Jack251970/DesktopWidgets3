using Microsoft.UI.Dispatching;

namespace DesktopWidgets3.Widget;

public interface IWidgetService
{
    DispatcherQueue GetDispatcherQueue(string widgetId);

    Task UpdateWidgetSettingsAsync(string widgetId, BaseWidgetSettings settings);
}
