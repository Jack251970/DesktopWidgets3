using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;

namespace DesktopWidgets3.ViewModels.Pages;

public partial class WidgetStorePageViewModel(DispatcherQueue dispatcherQueue, IExtensionService extensionService, IWidgetResourceService widgetResourceService) : ObservableRecipient, INavigationAware
{
    public ObservableCollection<WidgetStoreItem> AvailableWidgets { get; set; } = [];
    public ObservableCollection<WidgetStoreItem> InstalledWidgets { get; set; } = [];

    private readonly DispatcherQueue _dispatcherQueue = dispatcherQueue;

    private readonly IExtensionService _extensionService = extensionService;
    private readonly IWidgetResourceService _widgetResourceService = widgetResourceService;

    private readonly SemaphoreSlim _availableWidgetsLock = new(1, 1);
    private readonly SemaphoreSlim _installedWidgetsLock = new(1, 1);

    private bool _isInitialized;

    #region Initialize

    private async Task InitializeAvailableWidgetsAsync()
    {
        // TODO(Future): Load available widgets from Github, not supported yet.
        var githubAvailableWidgets = new List<WidgetStoreItem>();
        var preinstalledAvailableWidgets = await _widgetResourceService.GetPreinstalledAvailableWidgetStoreItemsAsync();
        List<WidgetStoreItem> availableWidgets = [.. githubAvailableWidgets, .. preinstalledAvailableWidgets];

        await Task.CompletedTask;

        await _availableWidgetsLock.WaitAsync();

        AvailableWidgets.Clear();
        foreach (var widget in availableWidgets)
        {
            AvailableWidgets.Add(widget);
        }

        _availableWidgetsLock.Release();
    }

    private async Task InitializeInstalledWidgetsAsync()
    {
        var installedWidgets = await _widgetResourceService.GetInstalledWidgetStoreItemsAsync();

        await _installedWidgetsLock.WaitAsync();

        InstalledWidgets.Clear();
        foreach (var widget in installedWidgets)
        {
            InstalledWidgets.Add(widget);
        }

        _installedWidgetsLock.Release();
    }

    #endregion

    #region Update

    #region Microsoft

    #region Extension

    private void ExtensionService_OnPackageInstalled(object? sender, IExtensionWrapper extension)
    {
        _dispatcherQueue.TryEnqueue(async () =>
        {
            await _installedWidgetsLock.WaitAsync();

            var widgetStoreItem = await _widgetResourceService.GetWidgetStoreItemAsync(extension);
            if (widgetStoreItem != null)
            {
                InstalledWidgets.Add(widgetStoreItem);
            }

            _installedWidgetsLock.Release();
        });
    }

    private void ExtensionService_OnPackageUninstalled(object? sender, string packageFamilyName)
    {
        _dispatcherQueue.TryEnqueue(async () =>
        {
            await _installedWidgetsLock.WaitAsync();

            var widgetToRemove = InstalledWidgets.FirstOrDefault(x => x.FamilyName == packageFamilyName);
            if (widgetToRemove != null)
            {
                InstalledWidgets.Remove(widgetToRemove);
            }

            _installedWidgetsLock.Release();
        });
    }

    private void ExtensionService_OnPackageUpdated(object? sender, IExtensionWrapper extension)
    {
        _dispatcherQueue.TryEnqueue(async () =>
        {
            await _installedWidgetsLock.WaitAsync();

            var widgetToUpdate = InstalledWidgets.FirstOrDefault(x => x.FamilyName == extension.PackageFamilyName);
            if (widgetToUpdate != null)
            {
                var widgetIndex = InstalledWidgets.IndexOf(widgetToUpdate);
                InstalledWidgets.Remove(widgetToUpdate);
                var widgetStoreItem = await _widgetResourceService.GetWidgetStoreItemAsync(extension);
                if (widgetStoreItem != null)
                {
                    InstalledWidgets.Insert(widgetIndex, widgetStoreItem);
                }
            }

            _installedWidgetsLock.Release();
        });
    }

    #endregion

    #endregion

    #endregion

    #region Navigation Aware

    public async void OnNavigatedTo(object parameter)
    {
        if (!_isInitialized)
        {
            await InitializeAvailableWidgetsAsync();
            await InitializeInstalledWidgetsAsync();

            _extensionService.OnPackageInstalled += ExtensionService_OnPackageInstalled;
            _extensionService.OnPackageUpdated += ExtensionService_OnPackageUpdated;
            _extensionService.OnPackageUninstalled += ExtensionService_OnPackageUninstalled;

            _isInitialized = true;

            return;
        }
    }

    public void OnNavigatedFrom()
    {

    }

    #endregion
}
