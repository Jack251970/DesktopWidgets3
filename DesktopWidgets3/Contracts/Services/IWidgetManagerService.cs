﻿using DesktopWidgets3.Models;

namespace DesktopWidgets3.Contracts.Services;

public interface IWidgetManagerService
{
    void InitializeWidgets();

    void ShowWidget(WidgetType widgetType);

    void CloseWidget(WidgetType widgetType);

    void CloseAllWidgets();

    Task SetThemeAsync();

    List<DashboardWidgetItem> GetAllWidgets(Action<DashboardWidgetItem>? EnabledChangedCallback);
}
