// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Dashboard.ComSafeWidgetObjects;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.Widgets.Hosts;

namespace DevHome.Dashboard.Services;

public interface IWidgetIconService
{
    public void RemoveIconsFromMicrosoftProviderIconCache(string providerDefinitionId);

    public Task<Brush> GetBrushForMicrosoftWidgetProviderIconAsync(WidgetProviderDefinition widgetProviderDefinition);

    public void RemoveIconsFromMicrosoftIconCache(string definitionId);

    public Task<Brush> GetBrushForMicrosoftWidgetIconAsync(ComSafeWidgetDefinition widgetDefinition, ElementTheme actualTheme);
}
