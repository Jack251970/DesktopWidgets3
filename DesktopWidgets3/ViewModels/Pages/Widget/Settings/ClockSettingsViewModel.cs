﻿using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Contracts.ViewModels;
using DesktopWidgets3.Models.Widget;

namespace DesktopWidgets3.ViewModels.Pages.Widget.Settings;

public partial class ClockSettingsViewModel : ObservableRecipient, INavigationAware
{
    [ObservableProperty]
    private bool _showSeconds = true;

    private readonly INavigationService _navigationService;
    private readonly IWidgetManagerService _widgetManagerService;

    private readonly WidgetType widgetType = WidgetType.Clock;
    private int indexTag = -1;

    private BaseWidgetSettings? _widgetSettings;

    private bool _isInitialized;

    public ClockSettingsViewModel(INavigationService navigationService, IWidgetManagerService widgetManagerService)
    {
        _navigationService = navigationService;
        _widgetManagerService = widgetManagerService;
    }

    public void InitializeSettings()
    {
        var settings = _widgetSettings as ClockWidgetSettings;
        ShowSeconds = settings!.ShowSeconds;
    }

    public async void OnNavigatedTo(object parameter)
    {
        if (parameter is Dictionary<string, object> param)
        {
            if (param.TryGetValue("WidgetType", out var widgetTypeObj) && (WidgetType)widgetTypeObj == widgetType && param.TryGetValue("IndexTag", out var indexTagObj))
            {
                indexTag = (int)indexTagObj;
                _widgetSettings = await _widgetManagerService.GetWidgetSettings(widgetType, indexTag);
                if (_widgetSettings != null)
                {
                    InitializeSettings();
                    _isInitialized = true;
                }
            }
        }

        if (!_isInitialized)
        {
            var dashboardPageKey = typeof(DashboardViewModel).FullName!;
            _navigationService.NavigateTo(dashboardPageKey);
        }
    }

    public void OnNavigatedFrom()
    {
        
    }

    partial void OnShowSecondsChanged(bool value)
    {
        if (_isInitialized)
        {
            var settings = _widgetSettings as ClockWidgetSettings;
            settings!.ShowSeconds = value;
            _widgetManagerService.UpdateWidgetSettings(widgetType, indexTag, settings);
        }
    }
}
