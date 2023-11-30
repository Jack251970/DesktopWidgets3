using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Contracts.ViewModels;
using DesktopWidgets3.Models.Widget;

namespace DesktopWidgets3.ViewModels.Pages;

public partial class DashboardViewModel : ObservableRecipient, INavigationAware
{
    public ObservableCollection<DashboardWidgetItem> AllWidgets { get; set; } = new();
    public ObservableCollection<DashboardWidgetItem> EnabledWidgets { get; set; } = new();
    public ObservableCollection<DashboardWidgetItem> DisabledWidgets { get; set; } = new();

    private readonly IAppSettingsService _appSettingsService;
    private readonly IWidgetManagerService _widgetManagerService;

    private List<DashboardWidgetItem> yourWidgetItems = new();

    private bool _isInitialized;

    public DashboardViewModel(IAppSettingsService appSettingsService, IWidgetManagerService widgetManagerService)
    {
        _appSettingsService = appSettingsService;
        _widgetManagerService = widgetManagerService;

        LoadAllWidgets();
    }

    public async void OnNavigatedTo(object parameter)
    {
        if (!_isInitialized)
        {
            yourWidgetItems = await _widgetManagerService.GetYourWidgetItemsAsync();

            foreach (var item in yourWidgetItems)
            {
                item.EnabledChangedCallback = EnabledChangedOnUI;
            }

            RefreshYourWidgets();

            _isInitialized = true;
        }
    }

    public void OnNavigatedFrom()
    {
        
    }

    public async void AllWidgetsItemClick(WidgetType widgetType)
    {
        await _widgetManagerService.ShowWidget(widgetType, null);

        var indexTag = _widgetManagerService.GetIndexTag();
        /*yourWidgetItems.Add(new DashboardWidgetItem
        {
            Type = widgetType,
            IndexTag = indexTag,
            IsEnabled = true,
            EnabledChangedCallback = EnabledChangedOnUI
        });*/

        RefreshYourWidgets();
    }

    private async void EnabledChangedOnUI(DashboardWidgetItem dashboardListItem)
    {
        if (dashboardListItem.IsEnabled)
        {
            await _widgetManagerService.ShowWidget(dashboardListItem.Type, dashboardListItem.IndexTag);
            yourWidgetItems.First(x => x.Type == dashboardListItem.Type).IsEnabled = true;
        }
        else
        {
            await _widgetManagerService.CloseWidget(dashboardListItem.Type, dashboardListItem.IndexTag);
            yourWidgetItems.First(x => x.Type == dashboardListItem.Type).IsEnabled = false;
        }

        RefreshYourWidgets();
    }

    private void LoadAllWidgets()
    {
        var allWidgets = _widgetManagerService.GetAllWidgetItems();

        AllWidgets.Clear();

        foreach (var item in allWidgets)
        {
            AllWidgets.Add(item);
        }
    }

    private void RefreshYourWidgets()
    {
        EnabledWidgets.Clear();
        DisabledWidgets.Clear();

        foreach (var item in yourWidgetItems)
        {
            if (item.IsEnabled)
            {
                EnabledWidgets.Add(item);
            }
            else
            {
                DisabledWidgets.Add(item);
            }
        }
    }
}
