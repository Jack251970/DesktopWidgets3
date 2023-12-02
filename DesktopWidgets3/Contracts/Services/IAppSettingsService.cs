using DesktopWidgets3.Models.Widget;

namespace DesktopWidgets3.Contracts.Services;

public interface IAppSettingsService
{
    bool BatterySaver
    {
        get; set;
    }

    Task SetBatterySaverAsync(bool value);

    Task<List<JsonWidgetItem>> GetWidgetsList();

    Task UpdateWidgetsList(JsonWidgetItem item);

    Task UpdateWidgetsList(List<JsonWidgetItem> widgetList);

    Task DeleteWidgetsList(JsonWidgetItem widgetItem);
}
