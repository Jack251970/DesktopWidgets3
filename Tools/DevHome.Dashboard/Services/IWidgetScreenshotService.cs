// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Dashboard.ComSafeWidgetObjects;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace DevHome.Dashboard.Services;

public interface IWidgetScreenshotService
{
    public void RemoveScreenshotsFromMicrosoftIconCache(string definitionId);

    public Task<Brush> GetBrushForMicrosoftWidgetScreenshotAsync(ComSafeWidgetDefinition widgetDefinition, ElementTheme actualTheme);
}
