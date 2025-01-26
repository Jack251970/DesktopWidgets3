using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Serilog;

namespace DesktopWidgets3.ViewModels.Pages;

public partial class DashboardPageViewModel(DispatcherQueue dispatcherQueue, MicrosoftWidgetModel microsoftWidgetModel, WidgetViewModelFactory widgetViewModelFactory, IWidgetManagerService widgetManagerService, IWidgetResourceService widgetResourceService, IThemeSelectorService themeSelectorService) : ObservableRecipient, INavigationAware
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(DashboardPageViewModel));

    public ObservableCollection<DashboardWidgetItem> PinnedWidgets { get; set; } = [];
    public ObservableCollection<DashboardWidgetItem> UnpinnedWidgets { get; set; } = [];

    private readonly SemaphoreSlim _refreshWidgetsLock = new(1, 1);

    private readonly DispatcherQueue _dispatcherQueue = dispatcherQueue;
    private readonly MicrosoftWidgetModel _microsoftWidgetModel = microsoftWidgetModel;
    private readonly WidgetViewModelFactory _widgetViewModelFactory = widgetViewModelFactory;

    private readonly IWidgetManagerService _widgetManagerService = widgetManagerService;
    private readonly IWidgetResourceService _widgetResourceService = widgetResourceService;
    private readonly IThemeSelectorService _themeSelectorService = themeSelectorService;

    private List<DashboardWidgetItem> yourWidgets = null!;

    private bool _isInitialized;

    #region Command

    [RelayCommand]
    private async Task AddWidgetAsync()
    {
        var dialog = new AddWidgetDialog()
        {
            // XamlRoot must be set in the case of a ContentDialog running in a Desktop app.
            XamlRoot = App.MainWindow.Content.XamlRoot,
        };
        await dialog.ShowAsync();

        var newWidget = dialog.AddedWidget;

        if (newWidget != null)
        {
            if (newWidget is DesktopWidgets3WidgetDefinition newWidgetDefinition)
            {
                await _widgetManagerService.AddWidgetAsync(newWidgetDefinition.WidgetId, newWidgetDefinition.WidgetType, RefreshAddedWidgetAsync, false);
            }
            else if (newWidget is ComSafeWidgetDefinition newWidgetDefinition1)
            {
                await _microsoftWidgetModel.AddWidgetsAsync(newWidgetDefinition1, true, (wvm) =>
                {
                    return _widgetManagerService.AddWidgetAsync(wvm, RefreshAddedWidgetAsync, false);
                });
            }
        }
    }

    #endregion

    #region Initialize

    private async Task InitializeYourWidgetsAsync()
    {
        yourWidgets = await _widgetResourceService.GetYourDashboardWidgetItemsAsync(_themeSelectorService.GetActualTheme());
        foreach (var item in yourWidgets)
        {
            item.PinnedChangedCallback = OnPinnedChanged;
        }

        RefreshYourWidgets();
    }

    private async void OnPinnedChanged(DashboardWidgetItem item)
    {
        if (item.Pinned)
        {
            await PinWidgetItemAsync(item.ProviderType, item.Id, item.Type, item.Index);
            await _widgetManagerService.PinWidgetAsync(item.ProviderType, item.Id, item.Type, item.Index, false);
        }
        else
        {
            await UnpinWidgetItemAsync(item.ProviderType, item.Id, item.Type, item.Index);
            await _widgetManagerService.UnpinWidgetAsync(item.ProviderType, item.Id, item.Type, item.Index, false);
        }
    }

    #endregion

    #region Refresh

    internal async Task RefreshDeletedWidgetAsync(WidgetProviderType providerType, string widgetId, string widgetType, int widgetIndex)
    {
        await _refreshWidgetsLock.WaitAsync();

        var widgetToDelete = PinnedWidgets.FirstOrDefault(x => x.Equals(providerType, widgetId, widgetType, widgetIndex));
        if (widgetToDelete != null)
        {
            PinnedWidgets.Remove(widgetToDelete);
        }

        var widgetToDelete1 = UnpinnedWidgets.FirstOrDefault(x => x.Equals(providerType, widgetId, widgetType, widgetIndex));
        if (widgetToDelete1 != null)
        {
            UnpinnedWidgets.Remove(widgetToDelete1);
        }

        var index = yourWidgets.FindIndex(x => x.Equals(providerType, widgetId, widgetType, widgetIndex));
        if (index != -1)
        {
            yourWidgets.RemoveAt(index);
        }

        _refreshWidgetsLock.Release();
    }

    private async Task RefreshAddedWidgetAsync(string widgetId, string widgetType, int widgetIndex)
    {
        var widgetItem = await _widgetResourceService.GetDashboardWidgetItemAsync(widgetId, widgetType, widgetIndex, _themeSelectorService.GetActualTheme());
        if (widgetItem != null)
        {
            widgetItem.PinnedChangedCallback = OnPinnedChanged;
            await AddWidgetItemAsync(widgetItem);
        }
    }

    private async Task RefreshAddedWidgetAsync(string widgetId, string widgetType, int widgetIndex, WidgetViewModel widgetViewModel)
    {
        var widgetItem = await _widgetResourceService.GetDashboardWidgetItemAsync(widgetId, widgetType, widgetIndex, widgetViewModel, _themeSelectorService.GetActualTheme());
        if (widgetItem != null)
        {
            widgetItem.PinnedChangedCallback = OnPinnedChanged;
            await AddWidgetItemAsync(widgetItem);
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

    #region Add & Pin & Unpin

    private async Task AddWidgetItemAsync(DashboardWidgetItem item)
    {
        await _refreshWidgetsLock.WaitAsync();

        if (item.Pinned)
        {
            PinnedWidgets.Add(item);
        }
        else
        {
            UnpinnedWidgets.Add(item);
        }

        yourWidgets.Add(item);

        _refreshWidgetsLock.Release();
    }

    private async Task PinWidgetItemAsync(WidgetProviderType providerType, string widgetId, string widgetType, int widgetIndex)
    {
        await _refreshWidgetsLock.WaitAsync();

        var indexToPin = yourWidgets.FindIndex(x => x.Equals(providerType, widgetId, widgetType, widgetIndex));
        if (indexToPin != -1)
        {
            yourWidgets[indexToPin].Pinned = true;
            yourWidgets[indexToPin].PinnedChangedCallback = OnPinnedChanged;
            RefreshYourWidgets();
        }

        _refreshWidgetsLock.Release();
    }

    private async Task UnpinWidgetItemAsync(WidgetProviderType providerType, string widgetId, string widgetType, int widgetIndex)
    {
        await _refreshWidgetsLock.WaitAsync();

        var indexToUnpin = yourWidgets.FindIndex(x => x.Equals(providerType, widgetId, widgetType, widgetIndex));
        if (indexToUnpin != -1)
        {
            yourWidgets[indexToUnpin].Pinned = false;
            yourWidgets[indexToUnpin].PinnedChangedCallback = OnPinnedChanged;
            RefreshYourWidgets();
        }

        _refreshWidgetsLock.Release();
    }

    #endregion

    #region Navigation Aware

    public async void OnNavigatedTo(object parameter)
    {
        if (!_isInitialized)
        {
            await InitializeYourWidgetsAsync();

            _widgetManagerService.AllowMultipleWidgetChanged += WidgetManagerService_AllowMultipleWidgetChanged;

            _isInitialized = true;

            return;
        }

        if (parameter is DashboardViewModelNavigationParameter navigationParameter)
        {
            var providerType = navigationParameter.ProviderType;
            var widgetId = navigationParameter.Id;
            var widgetType = navigationParameter.Type;
            var widgetIndex = navigationParameter.Index;
            var actualTheme = _themeSelectorService.GetActualTheme();
            switch (navigationParameter.Event)
            {
                case DashboardViewModelNavigationParameter.UpdateEvent.Add:
                    if (providerType == WidgetProviderType.DesktopWidgets3)
                    {
                        var widgetItem = await _widgetResourceService.GetDashboardWidgetItemAsync(widgetId, widgetType, widgetIndex, actualTheme);
                        if (widgetItem != null)
                        {
                            widgetItem.PinnedChangedCallback = OnPinnedChanged;
                            await AddWidgetItemAsync(widgetItem);
                        }
                    }
                    else
                    {
                        var widgetViewModel = _widgetManagerService.GetWidgetViewModel(widgetId, widgetType, widgetIndex);
                        if (widgetViewModel != null)
                        {
                            var widgetItem = await _widgetResourceService.GetDashboardWidgetItemAsync(widgetId, widgetType, widgetIndex, widgetViewModel, actualTheme);
                            if (widgetItem != null)
                            {
                                widgetItem.PinnedChangedCallback = OnPinnedChanged;
                                await AddWidgetItemAsync(widgetItem);
                            }
                        }
                    }
                    break;
                case DashboardViewModelNavigationParameter.UpdateEvent.Pin:
                    await PinWidgetItemAsync(providerType, widgetId, widgetType, widgetIndex);
                    break;
                case DashboardViewModelNavigationParameter.UpdateEvent.Unpin:
                    await UnpinWidgetItemAsync(providerType, widgetId, widgetType, widgetIndex);
                    break;
                case DashboardViewModelNavigationParameter.UpdateEvent.Delete:
                    await RefreshDeletedWidgetAsync(providerType, widgetId, widgetType, widgetIndex);
                    break;
            }
        }
    }

    public void OnNavigatedFrom()
    {

    }

    #endregion

    #region Update Theme

    public async Task UpdateThemeAsync(ElementTheme actualTheme)
    {
        await _refreshWidgetsLock.WaitAsync();

        foreach (var widgetItem in yourWidgets)
        {
            widgetItem.IconFill = await _widgetResourceService.GetWidgetIconBrushAsync(
                widgetItem.ProviderType,
                widgetItem.Id,
                widgetItem.Type,
                actualTheme);
        }

        _refreshWidgetsLock.Release();
    }

    #endregion

    #region Update Editable

    private async void WidgetManagerService_AllowMultipleWidgetChanged(object? sender, EventArgs e)
    {
        await _refreshWidgetsLock.WaitAsync();

        foreach (var widgetItem in UnpinnedWidgets)
        {
            var providerType = widgetItem.ProviderType;
            var widgetId = widgetItem.Id;
            var widgetType = widgetItem.Type;
            widgetItem.Editable = (!widgetItem.IsUnknown) && widgetItem.IsInstalled && (!await _widgetManagerService.IsWidgetSingleInstanceAndAlreadyPinnedAsync(providerType, widgetId, widgetType));
        }

        _refreshWidgetsLock.Release();
    }

    #endregion
}
