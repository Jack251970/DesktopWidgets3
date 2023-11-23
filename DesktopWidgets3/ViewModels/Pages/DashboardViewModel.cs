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

    public ObservableCollection<WidgetItem> EnabledWidgets { get; set; } = new();

    public ObservableCollection<WidgetItem> DisabledWidgets { get; set; } = new();

    public DashboardViewModel(ILocalSettingsService localSettingsService, IWidgetManagerService widgetManagerService)
    {
        _localSettingsService = localSettingsService;
        _widgetManagerService = widgetManagerService;

        var _allModules = _widgetManagerService.GetAllWidgets(EnabledChangedOnUI);

        EnabledWidgets = new ObservableCollection<WidgetItem>(_allModules.Where(x => x.IsEnabled));
        DisabledWidgets = new ObservableCollection<WidgetItem>(_allModules.Where(x => !x.IsEnabled));
    }

    private void EnabledChangedOnUI(WidgetItem dashboardListItem)
    {
        // Views.ShellPage.UpdateGeneralSettingsCallback(dashboardListItem.Tag, dashboardListItem.IsEnabled);

        if (dashboardListItem.IsEnabled)
        {
            _widgetManagerService.ShowWidget(dashboardListItem.Tag);
        }
        else
        {
            _widgetManagerService.CloseWidget(dashboardListItem.Tag);
        }
    }

    public void WidgetsEnabledChanged()
    {
        EnabledWidgets.Clear();
        DisabledWidgets.Clear();

        List<WidgetItem> _allModules = new();
        foreach (WidgetType moduleType in Enum.GetValues(typeof(WidgetType)))
        {
            _allModules.Add(new WidgetItem()
            {
                Tag = moduleType,
                Label = moduleType.ToString(),
                IsEnabled = true,
                Icon = null,
                EnabledChangedCallback = EnabledChangedOnUI,
                //DashboardModuleItems = null,
            });
        }

        EnabledWidgets = new ObservableCollection<WidgetItem>(_allModules.Where(x => x.IsEnabled));
        DisabledWidgets = new ObservableCollection<WidgetItem>(_allModules.Where(x => !x.IsEnabled));

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

    }

    public void OnNavigatedFrom()
    {
        
    }
}
