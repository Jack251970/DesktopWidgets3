using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Contracts.ViewModels;
using DesktopWidgets3.Models;

namespace DesktopWidgets3.ViewModels.Pages;

public partial class DashboardViewModel : ObservableRecipient, INavigationAware
{
    private readonly ILocalSettingsService _localSettingsService;
    private readonly IWidgetManagerService _widgetManagerService;

    public ObservableCollection<DashboardListItem> EnabledWidgets { get; set; } = new();

    public ObservableCollection<DashboardListItem> DisabledWidgets { get; set; } = new();

#if DEBUG
    private static int i = 0;
#endif

    public DashboardViewModel(ILocalSettingsService localSettingsService, IWidgetManagerService widgetManagerService)
    {
        _localSettingsService = localSettingsService;
        _widgetManagerService = widgetManagerService;

        // _widgetManagerService.ShowWidget(WidgetType.Clock);
        List<DashboardListItem> _allModules = new();

        int i = 0;
        foreach (WidgetType moduleType in Enum.GetValues(typeof(WidgetType)))
        {
            _allModules.Add(new DashboardListItem()
            {
                Tag = moduleType,
                Label = moduleType.ToString(),
                IsEnabled = (i < 6),
                Icon = null,
                EnabledChangedCallback = EnabledChangedOnUI,
                //DashboardModuleItems = null,
            });
            i++;
        }

        EnabledWidgets = new ObservableCollection<DashboardListItem>(_allModules.Where(x => x.IsEnabled));
        DisabledWidgets = new ObservableCollection<DashboardListItem>(_allModules.Where(x => !x.IsEnabled));
    }

    private void EnabledChangedOnUI(DashboardListItem dashboardListItem)
    {
        // Views.ShellPage.UpdateGeneralSettingsCallback(dashboardListItem.Tag, dashboardListItem.IsEnabled);
    }

    public void WidgetsEnabledChanged()
    {
        EnabledWidgets.Clear();
        DisabledWidgets.Clear();

        List<DashboardListItem> _allModules = new();
        foreach (WidgetType moduleType in Enum.GetValues(typeof(WidgetType)))
        {
            _allModules.Add(new DashboardListItem()
            {
                Tag = moduleType,
                Label = moduleType.ToString(),
                IsEnabled = true,
                Icon = null,
                EnabledChangedCallback = EnabledChangedOnUI,
                //DashboardModuleItems = null,
            });
        }

        EnabledWidgets = new ObservableCollection<DashboardListItem>(_allModules.Where(x => x.IsEnabled));
        DisabledWidgets = new ObservableCollection<DashboardListItem>(_allModules.Where(x => !x.IsEnabled));

        /*
        foreach (DashboardListItem item in _allModules)
        {
            item.IsEnabled = ModuleHelper.GetIsModuleEnabled(generalSettingsConfig, item.Tag);
            if (item.IsEnabled)
            {
                EnabledWidgets.Add(item);
            }
            else
            {
                DisabledWidgets.Add(item);
            }
        }

        OnPropertyChanged(nameof(ActiveModules));
        OnPropertyChanged(nameof(DisabledModules));*/

        // Change in local settings
    }

    public void OnNavigatedTo(object parameter)
    {
#if DEBUG
        // for test only: run in debug mode
        /*if (i == 0)
        {
            _widgetManagerService.ShowWidget(WidgetType.Clock);
            i++;
        }
        else if (i == 1)
        {
            _widgetManagerService.ShowWidget(WidgetType.CPU);
            i++;
        }
        else
        {
            i++;
        }*/
#endif
    }

    public void OnNavigatedFrom()
    {
        
    }
}
