﻿namespace DesktopWidgets3.Core.Widgets.Contracts.Services;

public interface IWidgetManagerService
{
    Task InitializeAsync();

    Task<int> AddWidgetAsync(string widgetId, bool refresh);

    Task EnableWidgetAsync(string widgetId, int indexTag);

    Task DisableWidgetAsync(string widgetId, int indexTag);

    Task DeleteWidgetAsync(string widgetId, int indexTag, bool refresh);

    Task DisableAllWidgetsAsync();

    bool IsWidgetEnabled(string widgetId, int indexTag);

    BaseWidgetViewModel? GetWidgetViewModel(WidgetWindow widgetWindow);

    void NavigateToWidgetSettingPage(string widgetId, int indexTag);

    void EnterEditMode();

    void SaveAndExitEditMode();

    void CancelAndExitEditMode();

    BaseWidgetSettings? GetWidgetSettings(string widgetId, int indexTag);

    Task UpdateWidgetSettingsAsync(string widgetId, int indexTag, BaseWidgetSettings settings, bool loadSettings);
}
