// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Dashboard.ComSafeWidgetObjects;
using DevHome.Dashboard.Services;
using Microsoft.UI.Xaml.Media;

namespace DevHome.Dashboard.ViewModels;

public partial class AddWidgetViewModel(
    IWidgetScreenshotService widgetScreenshotService,
    IThemeSelectorService themeSelectorService) : ObservableObject
{
    private readonly IWidgetScreenshotService _widgetScreenshotService = widgetScreenshotService;
    private readonly IThemeSelectorService _themeSelectorService = themeSelectorService;

    [ObservableProperty]
    private string _widgetDisplayTitle;

    [ObservableProperty]
    private string _widgetProviderDisplayTitle;

    [ObservableProperty]
    private Brush _widgetScreenshot;

    [ObservableProperty]
    private bool _pinButtonVisibility;

    private object _selectedWidgetDefinition;

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
        WidgetScreenshot = await _widgetScreenshotService.GetBrushForDesktopWidgets3WidgetScreenshotAsync(selectedWidgetDefinition, _themeSelectorService.GetActualTheme());
        PinButtonVisibility = true;
    }

    public void Clear()
    {
        WidgetDisplayTitle = string.Empty;
        WidgetProviderDisplayTitle = string.Empty;
        WidgetScreenshot = null;
        PinButtonVisibility = false;
        _selectedWidgetDefinition = null;
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
                WidgetScreenshot = await _widgetScreenshotService.GetBrushForDesktopWidgets3WidgetScreenshotAsync(selectedWidgetDefinition1, theme);
            }
        }
    }
}
