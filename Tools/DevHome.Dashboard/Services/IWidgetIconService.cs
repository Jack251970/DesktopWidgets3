// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Dashboard.ComSafeWidgetObjects;
using Microsoft.UI.Xaml.Media;

namespace DevHome.Dashboard.Services;

public interface IWidgetIconService
{
    // TODO: Check clean all caches. Introduce functions in DashboardView.xaml.cs to call these functions.
    public void RemoveIconsFromDesktopWidgets3Cache(string widgetId, string widgetType);

    // TODO: Move to the Core.Widgets project and use JsonItem instead.
    public Task<Brush> GetBrushForDesktopWidgets3WidgetIconAsync(string widgetId, string widgetType);

    public void RemoveIconsFromMicrosoftCache(string definitionId);

    public Task<Brush> GetBrushForMicrosoftWidgetIconAsync(ComSafeWidgetDefinition widgetDefinition);
}
