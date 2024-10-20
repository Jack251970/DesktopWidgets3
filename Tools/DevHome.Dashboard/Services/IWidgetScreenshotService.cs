// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using DevHome.Dashboard.ComSafeWidgetObjects;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace DevHome.Dashboard.Services;

public interface IWidgetScreenshotService
{
    // TODO: Check clean all caches.
    public void RemoveScreenshotsFromDesktopWidgets3Cache(string widgetId, string widgetType);

    public Task<Brush> GetBrushForDesktopWidgets3WidgetScreenshotAsync(DesktopWidgets3WidgetDefinition widgetDefinition, ElementTheme actualTheme);

    public void RemoveScreenshotsFromMicrosoftCache(string definitionId);

    public Task<Brush> GetBrushForMicrosoftWidgetScreenshotAsync(ComSafeWidgetDefinition widgetDefinition, ElementTheme actualTheme);
}
