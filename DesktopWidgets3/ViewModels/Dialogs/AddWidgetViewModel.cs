// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace DesktopWidgets3.ViewModels.Dialogs;

public partial class AddWidgetViewModel(
    DispatcherQueue dispatcherQueue,
    IWidgetResourceService widgetResourceService) : ObservableObject
{
    private readonly DispatcherQueue _dispatcherQueue = dispatcherQueue;
    private readonly IWidgetResourceService _widgetResourceService = widgetResourceService;

    [ObservableProperty]
    private string _widgetDisplayTitle = string.Empty;

    [ObservableProperty]
    private string _widgetProviderDisplayTitle = string.Empty;

    [ObservableProperty]
    private Brush _widgetScreenshot = null!;

    [ObservableProperty]
    private bool _pinButtonVisibility;

    private object _selectedWidgetDefinition = null!;

    public async Task SetWidgetDefinition(ComSafeWidgetDefinition selectedWidgetDefinition, ElementTheme actualTheme)
    {
        _selectedWidgetDefinition = selectedWidgetDefinition;

        WidgetDisplayTitle = selectedWidgetDefinition.DisplayTitle;
        WidgetProviderDisplayTitle = selectedWidgetDefinition.ProviderDefinitionDisplayName;
        WidgetScreenshot = await _widgetResourceService.GetWidgetScreenshotBrushAsync(_dispatcherQueue, selectedWidgetDefinition, actualTheme);
        PinButtonVisibility = true;
    }

    public async Task SetWidgetDefinition(DesktopWidgets3WidgetDefinition selectedWidgetDefinition, ElementTheme actualTheme)
    {
        _selectedWidgetDefinition = selectedWidgetDefinition;

        WidgetDisplayTitle = selectedWidgetDefinition.DisplayTitle;
        WidgetProviderDisplayTitle = selectedWidgetDefinition.ProviderDefinitionDisplayName;
        WidgetScreenshot = await _widgetResourceService.GetWidgetScreenshotBrushAsync(
            _dispatcherQueue, 
            WidgetProviderType.DesktopWidgets3, 
            selectedWidgetDefinition.WidgetId, 
            selectedWidgetDefinition.WidgetType, 
            actualTheme);
        PinButtonVisibility = true;
    }

    public void Clear()
    {
        WidgetDisplayTitle = string.Empty;
        WidgetProviderDisplayTitle = string.Empty;
        WidgetScreenshot = null!;
        PinButtonVisibility = false;
        _selectedWidgetDefinition = null!;
    }

    public async Task UpdateThemeAsync(ElementTheme actualTheme)
    {
        if (_selectedWidgetDefinition != null)
        {
            // Update the preview image for the selected widget.
            if (_selectedWidgetDefinition as ComSafeWidgetDefinition is ComSafeWidgetDefinition selectedWidgetDefinition)
            {
                WidgetScreenshot = await _widgetResourceService.GetWidgetScreenshotBrushAsync(
                    _dispatcherQueue, 
                    selectedWidgetDefinition, 
                    actualTheme);
            }
            else if (_selectedWidgetDefinition as DesktopWidgets3WidgetDefinition is DesktopWidgets3WidgetDefinition selectedWidgetDefinition1)
            {
                WidgetScreenshot = await _widgetResourceService.GetWidgetScreenshotBrushAsync(
                    _dispatcherQueue,
                    WidgetProviderType.DesktopWidgets3,
                    selectedWidgetDefinition1.WidgetId, 
                    selectedWidgetDefinition1.WidgetType, 
                    actualTheme);
            }
        }
    }
}
