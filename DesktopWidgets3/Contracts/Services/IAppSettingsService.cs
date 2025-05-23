﻿using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Contracts.Services;

public interface IAppSettingsService
{
    void Initialize();

    Task<List<JsonWidgetItem>> InitializeWidgetListAsync();

    Task<List<JsonWidgetStoreItem>> InitializeWidgetStoreListAsync();

    string Language { get; }

    Task SetLanguageAsync(string language);

    bool SilentStart { get; }

    Task SetSilentStartAsync(bool value);

    bool BatterySaver { get; }

    event Action<bool>? OnBatterySaverChanged;

    Task SetBatterySaverAsync(bool value);

    ElementTheme Theme { get; }

    Task SetThemeAsync(ElementTheme theme);

    BackdropType BackdropType { get; }

    Task SetEnableMicrosoftWidgetsAsync(bool value);

    bool EnableMicrosoftWidgets { get; }

    Task SaveBackdropTypeInSettingsAsync(BackdropType type);

    List<JsonWidgetItem> GetWidgetsList();

    JsonWidgetItem? GetWidget(WidgetProviderType providerType, string widgetId, string widgetType, int widgetIndex);

    Task AddWidgetAsync(JsonWidgetItem item);

    Task DeleteWidgetAsync(WidgetProviderType providerType, string widgetId, string widgetType, int widgetIndex);

    Task PinWidgetAsync(WidgetProviderType providerType, string widgetId, string widgetType, int widgetIndex);

    Task UnpinWidgetAsync(WidgetProviderType providerType, string widgetId, string widgetType, int widgetIndex);

    Task UpdateWidgetSettingsAsync(WidgetProviderType providerType, string widgetId, string widgetType, int widgetIndex, BaseWidgetSettings settings);

    Task UpdateWidgetsListIgnoreSettingsAsync(List<JsonWidgetItem> list);

    List<JsonWidgetStoreItem> GetWidgetStoreList();

    Task SaveWidgetStoreListAsync(List<JsonWidgetStoreItem> list);
}
