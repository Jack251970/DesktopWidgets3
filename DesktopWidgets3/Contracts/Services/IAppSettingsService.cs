using DesktopWidgets3.Models;

namespace DesktopWidgets3.Contracts.Services;

public interface IAppSettingsService
{
    bool BatterySaver
    {
        get; set;
    }

    Task SetBatterySaverAsync(bool value);

    List<JsonWidgetItem> GetWidgetsList();

    Task SaveWidgetsList(JsonWidgetItem item);
}
