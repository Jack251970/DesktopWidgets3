﻿namespace DesktopWidgets3.Contracts.Services;

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

    Task<List<JsonWidgetItem>> GetWidgetsList();

    Task UpdateWidgetsList(JsonWidgetItem item);

    Task UpdateWidgetsListIgnoreSetting(List<JsonWidgetItem> widgetList);

    Task DeleteWidget(string widgetId, int indexTag);
}
