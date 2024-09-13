using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace DesktopWidgets3.ViewModels.Pages;

public partial class DashboardViewModel(IWidgetManagerService widgetManagerService, IWidgetResourceService widgetResourceService) : ObservableRecipient, INavigationAware
{
    public ObservableCollection<DashboardWidgetItem> AllWidgets { get; set; } = [];
    public ObservableCollection<DashboardWidgetItem> EnabledWidgets { get; set; } = [];
    public ObservableCollection<DashboardWidgetItem> DisabledWidgets { get; set; } = [];

    private readonly IWidgetManagerService _widgetManagerService = widgetManagerService;
    private readonly IWidgetResourceService _widgetResourceService = widgetResourceService;

    private List<DashboardWidgetItem> allWidgets = [];
    private List<DashboardWidgetItem> yourWidgets = [];

    private bool _isInitialized;

    #region Load

    private void LoadAllWidgets()
    {
        allWidgets = _widgetResourceService.GetInstalledDashboardItems();
    }

    private void LoadYourWidgets()
    {
        yourWidgets = _widgetResourceService.GetYourDashboardItems();
        foreach (var item in yourWidgets)
        {
            item.EnabledChangedCallback = WidgetEnabledChanged;
        }
    }

    private async void WidgetEnabledChanged(DashboardWidgetItem dashboardListItem)
    {
        if (dashboardListItem.IsEnabled)
        {
            await _widgetManagerService.EnableWidgetAsync(dashboardListItem.Id, dashboardListItem.IndexTag);
            yourWidgets.First(x => x.Id == dashboardListItem.Id && x.IndexTag == dashboardListItem.IndexTag).IsEnabled = true;
        }
        else
        {
            await _widgetManagerService.DisableWidgetAsync(dashboardListItem.Id, dashboardListItem.IndexTag);
            yourWidgets.First(x => x.Id == dashboardListItem.Id && x.IndexTag == dashboardListItem.IndexTag).IsEnabled = false;
        }

        RefreshYourWidgets();
    }

    #endregion

    #region Refresh

    internal void RefreshAddedWidget(string widgetId, int indexTag)
    {
        var widgetItem = _widgetResourceService.GetDashboardItem(widgetId, indexTag);
        if (widgetItem != null)
        {
            widgetItem.EnabledChangedCallback = WidgetEnabledChanged;
            yourWidgets.Add(widgetItem);

            RefreshYourWidgets();
        }
    }

    internal void RefreshDisabledWidget(string widgetId, int indexTag)
    {
        yourWidgets.Remove(yourWidgets.First(x => x.Id == widgetId && x.IndexTag == indexTag));

        RefreshYourWidgets();
    }

    private void RefreshAllWidgets()
    {
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

        foreach (var item in yourWidgets)
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

    #endregion

    #region Navigation Aware

    public void OnNavigatedTo(object parameter)
    {
        if (!_isInitialized)
        {
            LoadAllWidgets();
            RefreshAllWidgets();
            LoadYourWidgets();
            RefreshYourWidgets();

            _isInitialized = true;

            return;
        }

        if (parameter is DashboardViewModelNavigationParameter navigationParameter)
        {
            var widgetId = navigationParameter.Id;
            var indexTag = navigationParameter.IndexTag;
            switch (navigationParameter.Event)
            {
                case DashboardViewModelNavigationParameter.UpdateEvent.Add:
                    var widgetItem = _widgetResourceService.GetDashboardItem(widgetId, indexTag);
                    if (widgetItem != null)
                    {
                        widgetItem.EnabledChangedCallback = WidgetEnabledChanged;
                        yourWidgets.Add(widgetItem);

                        RefreshYourWidgets();
                    }
                    break;
                case DashboardViewModelNavigationParameter.UpdateEvent.Disable:
                    var widgetIndex = yourWidgets.FindIndex(x => x.Id == widgetId && x.IndexTag == indexTag);
                    if (widgetIndex != -1)
                    {
                        yourWidgets[widgetIndex].IsEnabled = false;
                        yourWidgets[widgetIndex].EnabledChangedCallback = WidgetEnabledChanged;

                        RefreshYourWidgets();
                    }
                    break;
                case DashboardViewModelNavigationParameter.UpdateEvent.Delete:
                    widgetIndex = yourWidgets.FindIndex(x => x.Id == widgetId && x.IndexTag == indexTag);
                    if (widgetIndex != -1)
                    {
                        yourWidgets.RemoveAt(widgetIndex);

                        RefreshYourWidgets();
                    }
                    break;
            }
        }
    }

    public void OnNavigatedFrom()
    {

    }

    #endregion
}
