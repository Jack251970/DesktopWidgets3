﻿using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Core.Widgets.Contracts.Services;

public interface IWidgetResourceService
{
    Task InitalizeAsync();

    Task DisposeWidgetsAsync();

    FrameworkElement GetWidgetFrameworkElement(string widgetId);

    Task EnableWidgetAsync(string widgetId, bool firstWidget);

    Task DisableWidgetAsync(string widgetId, bool lastWidget);

    BaseWidgetSettings GetDefaultSetting(string widgetId);

    FrameworkElement GetWidgetSettingFrameworkElement(string widgetId);

    RectSize GetDefaultSize(string widgetId);

    RectSize GetMinSize(string widgetId);

    RectSize GetMaxSize(string widgetId);

    bool GetWidgetInNewThread(string widgetId);

    List<DashboardWidgetItem> GetAllDashboardItems();

    List<DashboardWidgetItem> GetYourDashboardItemsAsync();

    DashboardWidgetItem GetDashboardItem(string widgetId, int indexTag);

    bool IsWidgetUnknown(string widgetId);

    List<WidgetStoreItem> GetInstalledWidgetStoreItems();

    List<WidgetStoreItem> GetPreinstalledAvailableWidgetStoreItems();

    Task InstallWidgetAsync(string widgetId);

    Task UninstallWidgetAsync(string widgetId);
}
