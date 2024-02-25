﻿using CommunityToolkit.Mvvm.ComponentModel;

namespace DesktopWidgets3.ViewModels.Pages.Widgets.Settings;

public abstract partial class BaseWidgetSettingsViewModel : ObservableRecipient, INavigationAware
{
    protected WidgetType WidgetType
    {
        get;
    }

    protected int IndexTag
    {
        get;
        private set;
    }

    protected BaseWidgetSettings? WidgetSettings
    {
        get;
        private set;
    }

    private readonly INavigationService _navigationService;
    private readonly IWidgetManagerService _widgetManagerService;

    protected bool IsInitialized
    {
        get;
        private set;
    }

    protected bool IsEnabled => _widgetManagerService.IsWidgetEnabled(WidgetType, IndexTag);

    public BaseWidgetSettingsViewModel()
    {
        _navigationService = App.GetService<INavigationService>();
        _widgetManagerService = App.GetService<IWidgetManagerService>();

        WidgetType = InitializeWidgetType();
        IndexTag = -1;
    }

    public async void OnNavigatedTo(object parameter)
    {
        if (parameter is Dictionary<string, object> param)
        {
            if (param.TryGetValue("WidgetType", out var widgetTypeObj) && (WidgetType)widgetTypeObj == WidgetType && param.TryGetValue("IndexTag", out var indexTagObj))
            {
                IndexTag = (int)indexTagObj;
                WidgetSettings = await _widgetManagerService.GetWidgetSettings(WidgetType, IndexTag);
                if (WidgetSettings != null)
                {
                    InitializeWidgetSettings();
                    IsInitialized = true;
                }
            }
        }

        if (!IsInitialized)
        {
            var dashboardPageKey = typeof(DashboardViewModel).FullName!;
            _navigationService.NavigateTo(dashboardPageKey);
        }
    }

    public void OnNavigatedFrom()
    {

    }

    protected abstract WidgetType InitializeWidgetType();

    protected abstract void InitializeWidgetSettings();
}
