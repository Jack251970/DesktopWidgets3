using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DesktopWidgets3.ViewModels.Pages;

public partial class WidgetStoreViewModel(IWidgetResourceService widgetResourceService) : ObservableRecipient, INavigationAware
{
    public ObservableCollection<WidgetStoreItem> AvailableWidgets { get; set; } = [];
    public ObservableCollection<WidgetStoreItem> InstalledWidgets { get; set; } = [];

    private readonly IWidgetResourceService _widgetResourceService = widgetResourceService;

    private List<WidgetStoreItem> availableWidgets = [];
    private List<WidgetStoreItem> installedWidgets = [];

    private bool _isInitialized;

    #region Load

    private async Task LoadAvailableWidgets()
    {
        // TODO: Load available widgets from Github, not supported yet.
        var githubAvailableWidgets = new List<WidgetStoreItem>();

        var preinstalledAvailableWidgets = _widgetResourceService.GetPreinstalledAvailableWidgetStoreItems();

        availableWidgets = [.. githubAvailableWidgets, .. preinstalledAvailableWidgets];

        await Task.CompletedTask;
    }

    private void LoadInstalledWidgets()
    {
        installedWidgets = _widgetResourceService.GetInstalledWidgetStoreItems();
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
            await LoadAvailableWidgets();

            RefreshAvailableWidgets();

            LoadInstalledWidgets();

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
