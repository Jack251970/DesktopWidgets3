using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Contracts.ViewModels;
using DesktopWidgets3.Models;

namespace DesktopWidgets3.ViewModels.Pages;

public partial class DashboardViewModel : ObservableRecipient, INavigationAware
{
    public ObservableCollection<WidgetItem> EnabledWidgets { get; set; } = new();

    public ObservableCollection<WidgetItem> DisabledWidgets { get; set; } = new();

    private readonly ILocalSettingsService _localSettingsService;
    private readonly IWidgetManagerService _widgetManagerService;

    private readonly List<WidgetItem> allWidgetItems;

    public DashboardViewModel(ILocalSettingsService localSettingsService, IWidgetManagerService widgetManagerService)
    {
        _localSettingsService = localSettingsService;
        _widgetManagerService = widgetManagerService;

        allWidgetItems = _widgetManagerService.GetAllWidgets(EnabledChangedOnUI);

        EnabledWidgets = new ObservableCollection<WidgetItem>(allWidgetItems.Where(x => x.IsEnabled));
        DisabledWidgets = new ObservableCollection<WidgetItem>(allWidgetItems.Where(x => !x.IsEnabled));
    }

    public void OnNavigatedTo(object parameter)
    {

    }

    public void OnNavigatedFrom()
    {
        
    }

    private void EnabledChangedOnUI(WidgetItem dashboardListItem)
    {
        // Update widget
        if (dashboardListItem.IsEnabled)
        {
            _widgetManagerService.ShowWidget(dashboardListItem.Tag);
            allWidgetItems.First(x => x.Tag == dashboardListItem.Tag).IsEnabled = true;
        }
        else
        {
            _widgetManagerService.CloseWidget(dashboardListItem.Tag);
            allWidgetItems.First(x => x.Tag == dashboardListItem.Tag).IsEnabled = false;
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

        // Change in local settings
        // _localSettingsService.SetEnabledWidgets(allWidgetItems.Where(x => x.IsEnabled).Select(x => x.Tag).ToList());
    }
}
