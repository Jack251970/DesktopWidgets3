using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DesktopWidgets3.ViewModels.Pages;

public partial class WidgetStoreViewModel(IWidgetManagerService widgetManagerService, IWidgetResourceService widgetResourceService) : ObservableRecipient, INavigationAware
{
    public ObservableCollection<WidgetStoreItem> AvailableWidgets { get; set; } = [];
    public ObservableCollection<WidgetStoreItem> InstalledWidgets { get; set; } = [];

    private readonly IWidgetManagerService _widgetManagerService = widgetManagerService;
    private readonly IWidgetResourceService _widgetResourceService = widgetResourceService;

    private List<WidgetStoreItem> availableWidgets = [];
    private List<WidgetStoreItem> installedWidgets = [];

    private bool _isInitialized;

    public async void OnNavigatedTo(object parameter)
    {
        if (!_isInitialized)
        {
            await LoadAvailableWidgets();

            RefreshAvailableWidgets();

            installedWidgets = _widgetResourceService.GetInstalledWidgetStoreItems();

            RefreshInstalledWidgets();

            _isInitialized = true;

            return;
        }
    }

    public void OnNavigatedFrom()
    {

    }

    private async Task LoadAvailableWidgets()
    {
        // TODO: Load available widgets from Github, not supported yet
        var githubAvailableWidgets = new List<WidgetStoreItem>();

        // TODO
        var preinstalledAvailableWidgets = new List<WidgetStoreItem>();

        availableWidgets = [.. githubAvailableWidgets, .. preinstalledAvailableWidgets];

        await Task.CompletedTask;
    }

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
}
