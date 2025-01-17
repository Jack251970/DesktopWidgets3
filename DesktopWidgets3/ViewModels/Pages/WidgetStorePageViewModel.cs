using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DesktopWidgets3.ViewModels.Pages;

public partial class WidgetStorePageViewModel(IWidgetResourceService widgetResourceService) : ObservableRecipient, INavigationAware
{
    public ObservableCollection<WidgetStoreItem> AvailableWidgets { get; set; } = [];
    public ObservableCollection<WidgetStoreItem> InstalledWidgets { get; set; } = [];

    private readonly IWidgetResourceService _widgetResourceService = widgetResourceService;

    private List<WidgetStoreItem> availableWidgets = null!;
    private List<WidgetStoreItem> installedWidgets = null!;

    private bool _isInitialized;

    #region Load

    private async Task LoadAvailableWidgetsAsync()
    {
        // TODO(Future): Load available widgets from Github, not supported yet.
        var githubAvailableWidgets = new List<WidgetStoreItem>();
        var preinstalledAvailableWidgets = await _widgetResourceService.GetPreinstalledAvailableWidgetStoreItemsAsync();
        availableWidgets = [.. githubAvailableWidgets, .. preinstalledAvailableWidgets];

        await Task.CompletedTask;
    }

    private async Task LoadInstalledWidgetsAsync()
    {
        installedWidgets = await _widgetResourceService.GetInstalledWidgetStoreItemsAsync();
    }

    #endregion

    #region Refresh

    private void RefreshAvailableWidgets()
    {
        AvailableWidgets.Clear();
        foreach (var widget in availableWidgets)
        {
            AvailableWidgets.Add(widget);
        }
    }

    private void RefreshInstalledWidgets()
    {
        InstalledWidgets.Clear();
        foreach (var widget in installedWidgets)
        {
            InstalledWidgets.Add(widget);
        }
    }

    #endregion

    #region Navigation Aware

    public async void OnNavigatedTo(object parameter)
    {
        if (!_isInitialized)
        {
            await LoadAvailableWidgetsAsync();
            RefreshAvailableWidgets();
            await LoadInstalledWidgetsAsync();
            RefreshInstalledWidgets();

            _isInitialized = true;

            return;
        }
    }

    public void OnNavigatedFrom()
    {

    }

    #endregion
}
