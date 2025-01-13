// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media;

namespace DesktopWidgets3.ViewModels.Dialogs;

public partial class AddWidgetViewModel(
    IWidgetScreenshotService widgetScreenshotService,
    IThemeSelectorService themeSelectorService) : ObservableObject
{
    private readonly IWidgetScreenshotService _widgetScreenshotService = widgetScreenshotService;
    private readonly IThemeSelectorService _themeSelectorService = themeSelectorService;

    [ObservableProperty]
    private string _widgetDisplayTitle = string.Empty;

    [ObservableProperty]
    private string _widgetProviderDisplayTitle = string.Empty;

    [ObservableProperty]
    private Brush _widgetScreenshot = null!;

    [ObservableProperty]
    private bool _pinButtonVisibility;

    private object _selectedWidgetDefinition = null!;

    public async Task SetWidgetDefinition(ComSafeWidgetDefinition selectedWidgetDefinition)
    {
        _selectedWidgetDefinition = selectedWidgetDefinition;

        WidgetDisplayTitle = selectedWidgetDefinition.DisplayTitle;
        WidgetProviderDisplayTitle = selectedWidgetDefinition.ProviderDefinitionDisplayName;
        WidgetScreenshot = await _widgetScreenshotService.GetBrushForMicrosoftWidgetScreenshotAsync(selectedWidgetDefinition, _themeSelectorService.GetActualTheme());
        PinButtonVisibility = true;
    }

    public async Task SetWidgetDefinition(DesktopWidgets3WidgetDefinition selectedWidgetDefinition)
    {
        _selectedWidgetDefinition = selectedWidgetDefinition;

        WidgetDisplayTitle = selectedWidgetDefinition.DisplayTitle;
        WidgetProviderDisplayTitle = selectedWidgetDefinition.ProviderDefinitionDisplayName;
        WidgetScreenshot = await _widgetScreenshotService.GetBrushForDesktopWidgets3WidgetScreenshotAsync(selectedWidgetDefinition.WidgetId, selectedWidgetDefinition.WidgetType, _themeSelectorService.GetActualTheme());
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

    [RelayCommand]
    private async Task UpdateThemeAsync()
    {
        if (_selectedWidgetDefinition != null)
        {
            // Update the preview image for the selected widget.
            var theme = _themeSelectorService.GetActualTheme();
            if (_selectedWidgetDefinition as ComSafeWidgetDefinition is ComSafeWidgetDefinition selectedWidgetDefinition)
            {
                WidgetScreenshot = await _widgetScreenshotService.GetBrushForMicrosoftWidgetScreenshotAsync(selectedWidgetDefinition, theme);
            }
            else if (_selectedWidgetDefinition as DesktopWidgets3WidgetDefinition is DesktopWidgets3WidgetDefinition selectedWidgetDefinition1)
            {
                WidgetScreenshot = await _widgetScreenshotService.GetBrushForDesktopWidgets3WidgetScreenshotAsync(selectedWidgetDefinition1.WidgetId, selectedWidgetDefinition1.WidgetType, theme);
            }
        }
    }
}
