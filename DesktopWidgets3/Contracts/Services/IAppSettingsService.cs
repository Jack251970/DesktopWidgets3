namespace DesktopWidgets3.Contracts.Services;

public interface IAppSettingsService
{
    Task InitializeAsync();

    bool SilentStart { get; }

    Task SetSilentStartAsync(bool value);

    bool BatterySaver { get; }

    event Action<bool>? OnBatterySaverChanged;

    Task SetBatterySaverAsync(bool value);

    bool MultiThread { get; }

    Task SetMultiThreadAsync(bool value);

    Task<List<JsonWidgetItem>> InitializeWidgetListAsync();

    List<JsonWidgetItem> GetWidgetsList();

    JsonWidgetItem? GetWidget(string widgetId, int indexTag);

    Task AddWidgetAsync(JsonWidgetItem item);

    Task DeleteWidgetAsync(string widgetId, int indexTag);

    Task PinWidgetAsync(string widgetId, int indexTag);

    Task UnpinWidgetAsync(string widgetId, int indexTag);

    Task UpdateWidgetSettingsAsync(string widgetId, int indexTag, BaseWidgetSettings settings);

    Task UpdateWidgetsListIgnoreSettingsAsync(List<JsonWidgetItem> widgetList);

    Task<List<JsonWidgetStoreItem>> InitializeWidgetStoreListAsync();

    List<JsonWidgetStoreItem> GetWidgetStoreList();

    Task SaveWidgetStoreListAsync(List<JsonWidgetStoreItem> widgetStoreList);
}
