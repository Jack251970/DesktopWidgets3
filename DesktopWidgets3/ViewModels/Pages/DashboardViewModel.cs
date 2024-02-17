using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace DesktopWidgets3.ViewModels.Pages;

public partial class DashboardViewModel : ObservableRecipient, INavigationAware
{
    public enum UpdateEvent
    {
        Disable,
        Delete
    }

    public ObservableCollection<DashboardWidgetItem> AllWidgets { get; set; } = new();
    public ObservableCollection<DashboardWidgetItem> EnabledWidgets { get; set; } = new();
    public ObservableCollection<DashboardWidgetItem> DisabledWidgets { get; set; } = new();

    private readonly IWidgetManagerService _widgetManagerService;

    private List<DashboardWidgetItem> yourWidgetItems = new();

    private bool _isInitialized;

    public DashboardViewModel(IWidgetManagerService widgetManagerService)
    {
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
                item.EnabledChangedCallback = WidgetEnabledChanged;
            }

            RefreshYourWidgets();

            _isInitialized = true;
            return;
        }

        if (parameter is Dictionary<string, object> param)
        {
            if (param.TryGetValue("UpdateEvent", out var updateEventObj) && param.TryGetValue("WidgetType", out var widgetTypeObj) && param.TryGetValue("IndexTag", out var indexTagObj))
            {
                var updateEvent = (UpdateEvent)updateEventObj;
                var widgetType = (WidgetType)widgetTypeObj;
                var indexTag = (int)indexTagObj;
                
                if (updateEvent == UpdateEvent.Disable)
                {
                    var widgetItem = yourWidgetItems.First(x => x.Type == widgetType && x.IndexTag == indexTag);
                    widgetItem.IsEnabled = false;
                    widgetItem.EnabledChangedCallback = WidgetEnabledChanged;
                }
                else if (updateEvent == UpdateEvent.Delete)
                {
                    yourWidgetItems.Remove(yourWidgetItems.First(x => x.Type == widgetType && x.IndexTag == indexTag));
                }
            
                RefreshYourWidgets();
            }
        }
    }

    public void OnNavigatedFrom()
    {
        
    }

    internal async void AllWidgetsItemClick(WidgetType widgetType)
    {
        await _widgetManagerService.AddWidget(widgetType);

        var widgetItem = _widgetManagerService.GetCurrentEnabledWidget();
        widgetItem.IsEnabled = true;
        widgetItem.EnabledChangedCallback = WidgetEnabledChanged;
        yourWidgetItems.Add(widgetItem);

        RefreshYourWidgets();
    }

    internal async void MenuFlyoutItemDeleteWidgetClick(WidgetType widgetType, int indexTag)
    {
        await _widgetManagerService.DeleteWidget(widgetType, indexTag);

        yourWidgetItems.Remove(yourWidgetItems.First(x => x.Type == widgetType && x.IndexTag == indexTag));

        RefreshYourWidgets();
    }

    private async void WidgetEnabledChanged(DashboardWidgetItem dashboardListItem)
    {
        if (dashboardListItem.IsEnabled)
        {
            await _widgetManagerService.EnableWidget(dashboardListItem.Type, dashboardListItem.IndexTag);
            yourWidgetItems.First(x => x.Type == dashboardListItem.Type && x.IndexTag == dashboardListItem.IndexTag).IsEnabled = true;
        }
        else
        {
            await _widgetManagerService.DisableWidget(dashboardListItem.Type, dashboardListItem.IndexTag);
            yourWidgetItems.First(x => x.Type == dashboardListItem.Type && x.IndexTag == dashboardListItem.IndexTag).IsEnabled = false;
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
