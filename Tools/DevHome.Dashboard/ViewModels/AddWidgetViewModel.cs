// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Dashboard.ComSafeWidgetObjects;
using DevHome.Dashboard.Services;
using DevHome.Dashboard.Views;
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

    private AddedWidget _selectedWidget;

    public async Task SetWidgetDefinition(AddedWidget selectedWidget)
    {
        _selectedWidget = selectedWidget;
        var theme = _themeSelectorService.GetActualTheme();
        if (selectedWidget.WidgetDefination is ComSafeWidgetDefinition selectedWidgetDefinition)
        {
            WidgetDisplayTitle = selectedWidgetDefinition.DisplayTitle;
            WidgetProviderDisplayTitle = selectedWidgetDefinition.ProviderDefinitionDisplayName;
            WidgetScreenshot = await _widgetScreenshotService.GetBrushForMicrosoftWidgetScreenshotAsync(selectedWidgetDefinition, theme);
        }
        else
        {
            WidgetDisplayTitle = selectedWidget.WidgetName;
            WidgetProviderDisplayTitle = selectedWidget.WidgetGroupName;
            WidgetScreenshot = await _widgetScreenshotService.GetBrushForDesktopWidgets3WidgetScreenshotAsync(selectedWidget.WidgetId, selectedWidget.WidgetType, theme);
        }
        PinButtonVisibility = true;
    }

    public void Clear()
    {
        WidgetDisplayTitle = string.Empty;
        WidgetProviderDisplayTitle = string.Empty;
        WidgetScreenshot = null;
        PinButtonVisibility = false;
        _selectedWidget = null;
    }

    [RelayCommand]
    private async Task UpdateThemeAsync()
    {
        if (_selectedWidget != null)
        {
            // Update the preview image for the selected widget.
            var theme = _themeSelectorService.GetActualTheme();
            if (_selectedWidget.WidgetDefination is ComSafeWidgetDefinition selectedWidgetDefinition)
            {
                WidgetScreenshot = await _widgetScreenshotService.GetBrushForMicrosoftWidgetScreenshotAsync(selectedWidgetDefinition, theme);
            }
            else
            {
                WidgetScreenshot = await _widgetScreenshotService.GetBrushForDesktopWidgets3WidgetScreenshotAsync(_selectedWidget.WidgetId, _selectedWidget.WidgetType, theme);
            }
        }
    }
}
