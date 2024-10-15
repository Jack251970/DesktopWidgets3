using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace DesktopWidgets3.ViewModels.Pages;

public partial class DashboardViewModel(IWidgetManagerService widgetManagerService, IWidgetResourceService widgetResourceService) : ObservableRecipient, INavigationAware
{
    public ObservableCollection<DashboardWidgetItem> AllWidgets { get; set; } = [];
    public ObservableCollection<DashboardWidgetItem> PinnedWidgets { get; set; } = [];
    public ObservableCollection<DashboardWidgetItem> UnpinnedWidgets { get; set; } = [];

    private readonly IWidgetManagerService _widgetManagerService = widgetManagerService;
    private readonly IWidgetResourceService _widgetResourceService = widgetResourceService;

    private List<DashboardWidgetItem> allWidgets = null!;
    private List<DashboardWidgetItem> yourWidgets = null!;

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
            item.PinnedChangedCallback = OnPinnedChanged;
        }
    }

    private async void OnPinnedChanged(DashboardWidgetItem dashboardListItem)
    {
        if (dashboardListItem.Pinned)
        {
            await _widgetManagerService.PinWidgetAsync(dashboardListItem.Id, dashboardListItem.IndexTag);
            yourWidgets.First(x => x.Id == dashboardListItem.Id && x.IndexTag == dashboardListItem.IndexTag).Pinned = true;
        }
        else
        {
            await _widgetManagerService.UnpinWidgetAsync(dashboardListItem.Id, dashboardListItem.IndexTag, false);
            yourWidgets.First(x => x.Id == dashboardListItem.Id && x.IndexTag == dashboardListItem.IndexTag).Pinned = false;
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
            widgetItem.PinnedChangedCallback = OnPinnedChanged;
            yourWidgets.Add(widgetItem);

            RefreshYourWidgets();
        }
    }

    internal void RefreshUnpinnedWidget(string widgetId, int indexTag)
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
        PinnedWidgets.Clear();
        UnpinnedWidgets.Clear();
        foreach (var item in yourWidgets)
        {
            if (item.Pinned)
            {
                PinnedWidgets.Add(item);
            }
            else
            {
                UnpinnedWidgets.Add(item);
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
                        widgetItem.PinnedChangedCallback = OnPinnedChanged;
                        yourWidgets.Add(widgetItem);

                        RefreshYourWidgets();
                    }
                    break;
                case DashboardViewModelNavigationParameter.UpdateEvent.Unpin:
                    var widgetIndex = yourWidgets.FindIndex(x => x.Id == widgetId && x.IndexTag == indexTag);
                    if (widgetIndex != -1)
                    {
                        yourWidgets[widgetIndex].Pinned = false;
                        yourWidgets[widgetIndex].PinnedChangedCallback = OnPinnedChanged;

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
