namespace DesktopWidgets3.Contracts.Services;

public interface IAppSettingsService
{
    Task InitializeAsync();

    bool SilentStart { get; }

    Task SetSilentStartAsync(bool value);

    event EventHandler<bool>? OnBatterySaverChanged;

    bool BatterySaver { get; }

    Task SetBatterySaverAsync(bool value);

    bool MultiThread { get; }

    Task SetMultiThreadAsync(bool value);

    Task<List<JsonWidgetItem>> InitializeWidgetListAsync();

    List<JsonWidgetItem> GetWidgetsList();

    Task AddWidget(JsonWidgetItem item);

    Task DeleteWidget(string widgetId, int indexTag);

    Task<JsonWidgetItem> EnableWidget(string widgetId, int indexTag);

    Task DisableWidget(string widgetId, int indexTag);

    Task UpdateWidgetSettings(string widgetId, int indexTag, BaseWidgetSettings settings);

    Task UpdateWidgetsListIgnoreSettings(List<JsonWidgetItem> widgetList);

    Task<List<JsonWidgetStoreItem>> InitializeWidgetStoreListAsync();

    List<JsonWidgetStoreItem> GetWidgetStoreList();

    Task SaveWidgetStoreListAsync(List<JsonWidgetStoreItem> widgetStoreList);
}
