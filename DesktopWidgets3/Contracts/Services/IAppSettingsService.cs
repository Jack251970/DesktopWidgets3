using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Contracts.Services;

public interface IAppSettingsService
{
    void Initialize();

    string Language { get; }

    Task SaveLanguageInSettingsAsync(string language);

    bool SilentStart { get; }

    Task SetSilentStartAsync(bool value);

    bool BatterySaver { get; }

    event Action<bool>? OnBatterySaverChanged;

    Task SetBatterySaverAsync(bool value);

    bool MultiThread { get; }

    Task SetMultiThreadAsync(bool value);

    ElementTheme Theme { get; }

    Task SaveThemeInSettingsAsync(ElementTheme theme);

    BackdropType BackdropType { get; }

    Task SaveBackdropTypeInSettingsAsync(BackdropType type);

    Task<List<JsonWidgetItem>> InitializeWidgetListAsync();

    List<JsonWidgetItem> GetWidgetsList();

    JsonWidgetItem? GetWidget(string widgetId, string widgetType, int indexTag);

    Task AddWidgetAsync(JsonWidgetItem item);

    Task DeleteWidgetAsync(string widgetId, string widgetType, int indexTag);

    Task PinWidgetAsync(string widgetId, string widgetType, int indexTag);

    Task UnpinWidgetAsync(string widgetId, string widgetType, int indexTag);

    Task UpdateWidgetSettingsAsync(string widgetId, string widgetType, int indexTag, BaseWidgetSettings settings);

    Task UpdateWidgetsListIgnoreSettingsAsync(List<JsonWidgetItem> list);

    Task<List<JsonWidgetStoreItem>> InitializeWidgetStoreListAsync();

    List<JsonWidgetStoreItem> GetWidgetStoreList();

    Task SaveWidgetStoreListAsync(List<JsonWidgetStoreItem> list);
}
