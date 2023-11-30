using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Contracts.ViewModels;
using DesktopWidgets3.Models.Widget;

namespace DesktopWidgets3.ViewModels.Pages;

public partial class DashboardViewModel : ObservableRecipient, INavigationAware
{
    public ObservableCollection<DashboardWidgetItem> EnabledWidgets { get; set; } = new();

    public ObservableCollection<DashboardWidgetItem> DisabledWidgets { get; set; } = new();

    private readonly IAppSettingsService _appSettingsService;
    private readonly IWidgetManagerService _widgetManagerService;

    private readonly List<DashboardWidgetItem> allWidgetItems = new();

    private bool _isInitialized;

    public DashboardViewModel(IAppSettingsService appSettingsService, IWidgetManagerService widgetManagerService)
    {
        _appSettingsService = appSettingsService;
        _widgetManagerService = widgetManagerService;
    }

    public async void OnNavigatedTo(object parameter)
    {
        if (!_isInitialized)
        {
            var items = await _widgetManagerService.GetDashboardWidgetItemsAsync();

            foreach (var item in items)
            {
                allWidgetItems.Add(new DashboardWidgetItem
                {
                    Type = item.Type,
                    Label = item.Label,
                    Description = item.Description,
                    Icon = item.Icon,
                    IsEnabled = item.IsEnabled,
                    EnabledChangedCallback = EnabledChangedOnUI
                });
            }

            EnabledWidgets = new ObservableCollection<DashboardWidgetItem>(allWidgetItems.Where(x => x.IsEnabled));
            DisabledWidgets = new ObservableCollection<DashboardWidgetItem>(allWidgetItems.Where(x => !x.IsEnabled));

            _isInitialized = true;
        }
    }

    public void OnNavigatedFrom()
    {
        
    }

    private void EnabledChangedOnUI(DashboardWidgetItem dashboardListItem)
    {
        // Update widget
        if (dashboardListItem.IsEnabled)
        {
            _widgetManagerService.ShowWidget(dashboardListItem.Type, dashboardListItem.IndexTag);
            allWidgetItems.First(x => x.Type == dashboardListItem.Type).IsEnabled = true;
        }
        else
        {
            _widgetManagerService.CloseWidget(dashboardListItem.Type, dashboardListItem.IndexTag);
            allWidgetItems.First(x => x.Type == dashboardListItem.Type).IsEnabled = false;
        }

        // Reload lists
        EnabledWidgets.Clear();
        DisabledWidgets.Clear();

        foreach (var item in allWidgetItems)
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
