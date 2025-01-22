namespace DesktopWidgets3.Widget;

public interface IWidgetService
{
    Task UpdateWidgetSettingsAsync(string widgetId, BaseWidgetSettings settings);
}
