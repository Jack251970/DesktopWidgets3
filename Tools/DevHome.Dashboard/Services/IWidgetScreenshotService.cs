// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Dashboard.ComSafeWidgetObjects;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace DevHome.Dashboard.Services;

public interface IWidgetScreenshotService
{
    public void RemoveScreenshotsFromMicrosoftCache(string definitionId);

    public Task<Brush> GetBrushForMicrosoftWidgetScreenshotAsync(DispatcherQueue dispatcherQueue, ComSafeWidgetDefinition widgetDefinition, ElementTheme actualTheme);
}
