// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using DevHome.Dashboard.ComSafeWidgetObjects;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace DevHome.Dashboard.Services;

public interface IWidgetIconService
{
    // TODO: Check clean all caches.
    public void RemoveIconsFromDesktopWidgets3Cache(string widgetId, string widgetType);

    public Task<Brush> GetBrushForDesktopWidgets3WidgetIconAsync(string widgetId, string widgetType, ElementTheme actualTheme);

    public void RemoveIconsFromMicrosoftCache(string definitionId);

    public Task<Brush> GetBrushForMicrosoftWidgetIconAsync(ComSafeWidgetDefinition widgetDefinition, ElementTheme actualTheme);
}
