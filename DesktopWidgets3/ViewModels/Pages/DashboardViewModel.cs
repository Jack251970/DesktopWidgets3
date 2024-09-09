using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace DesktopWidgets3.ViewModels.Pages;

public partial class DashboardViewModel : ObservableRecipient, INavigationAware
{
    public enum UpdateEvent
    {
        Add,
        Disable,
        Delete
    }

    public ObservableCollection<DashboardWidgetItem> AllWidgets { get; set; } = [];
    public ObservableCollection<DashboardWidgetItem> EnabledWidgets { get; set; } = [];
    public ObservableCollection<DashboardWidgetItem> DisabledWidgets { get; set; } = [];

    private readonly IWidgetManagerService _widgetManagerService;
    private readonly IWidgetResourceService _widgetResourceService;

    private List<DashboardWidgetItem> yourWidgetItems = [];

    private bool _isInitialized;

    public DashboardViewModel(IWidgetManagerService widgetManagerService, IWidgetResourceService widgetResourceService)
    {
        _widgetManagerService = widgetManagerService;
        _widgetResourceService = widgetResourceService;

        LoadAllWidgets();
    }

    public async void OnNavigatedTo(object parameter)
    {
        if (!_isInitialized)
        {
            yourWidgetItems = await _widgetResourceService.GetYourDashboardItemsAsync();
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
            if (param.TryGetValue("UpdateEvent", out var updateEventObj) && param.TryGetValue("Id", out var widgetIdObj) && param.TryGetValue("IndexTag", out var indexTagObj))
            {
                var updateEvent = (UpdateEvent)updateEventObj;
                var widgetId = (string)widgetIdObj;
                var indexTag = (int)indexTagObj;
                
                if (updateEvent == UpdateEvent.Add)
                {
                    var widgetItem = _widgetResourceService.GetDashboardItem(widgetId, indexTag);
                    widgetItem.EnabledChangedCallback = WidgetEnabledChanged;
                    yourWidgetItems.Add(widgetItem);
                }
                else if (updateEvent == UpdateEvent.Disable)
                {
                    var widgetItem = yourWidgetItems.First(x => x.Id == widgetId && x.IndexTag == indexTag);
                    widgetItem.IsEnabled = false;
                    widgetItem.EnabledChangedCallback = WidgetEnabledChanged;
                }
                else if (updateEvent == UpdateEvent.Delete)
                {
                    yourWidgetItems.Remove(yourWidgetItems.First(x => x.Id == widgetId && x.IndexTag == indexTag));
                }
            
                RefreshYourWidgets();
            }
        }
    }

    public void OnNavigatedFrom()
    {
        
    }

    internal async void AllWidgetsItemClick(string widgetId)
    {
        await _widgetManagerService.AddWidget(widgetId);
    }

    internal async void MenuFlyoutItemDeleteWidgetClick(string widgetId, int indexTag)
    {
        await _widgetManagerService.DeleteWidget(widgetId, indexTag);

        yourWidgetItems.Remove(yourWidgetItems.First(x => x.Id == widgetId && x.IndexTag == indexTag));

        RefreshYourWidgets();
    }

    private async void WidgetEnabledChanged(DashboardWidgetItem dashboardListItem)
    {
        if (dashboardListItem.IsEnabled)
        {
            await _widgetManagerService.EnableWidget(dashboardListItem.Id, dashboardListItem.IndexTag);
            yourWidgetItems.First(x => x.Id == dashboardListItem.Id && x.IndexTag == dashboardListItem.IndexTag).IsEnabled = true;
        }
        else
        {
            await _widgetManagerService.DisableWidget(dashboardListItem.Id, dashboardListItem.IndexTag);
            yourWidgetItems.First(x => x.Id == dashboardListItem.Id && x.IndexTag == dashboardListItem.IndexTag).IsEnabled = false;
        }

        RefreshYourWidgets();
    }

    private void LoadAllWidgets()
    {
        var allWidgets = _widgetResourceService.GetAllDashboardItems();

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
