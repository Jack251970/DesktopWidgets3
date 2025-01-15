using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Serilog;
using System.Collections.ObjectModel;

namespace DesktopWidgets3.ViewModels.Pages;

public partial class DashboardPageViewModel(DispatcherQueue dispatcherQueue, MicrosoftWidgetModel microsoftWidgetModel, WidgetViewModelFactory widgetViewModelFactory, IWidgetExtensionService widgetExtensionService, IWidgetHostingService widgetHostingService, IWidgetManagerService widgetManagerService, IWidgetResourceService widgetResourceService, IThemeSelectorService themeSelectorService) : ObservableRecipient, INavigationAware
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(DashboardPageViewModel));

    public ObservableCollection<DashboardWidgetItem> PinnedWidgets { get; set; } = [];
    public ObservableCollection<DashboardWidgetItem> UnpinnedWidgets { get; set; } = [];

    private readonly DispatcherQueue _dispatcherQueue = dispatcherQueue;
    private readonly MicrosoftWidgetModel _microsoftWidgetModel = microsoftWidgetModel;
    private readonly WidgetViewModelFactory _widgetViewModelFactory = widgetViewModelFactory;

    private readonly IWidgetExtensionService _widgetExtensionService = widgetExtensionService;
    private readonly IWidgetHostingService _widgetHostingService = widgetHostingService;
    private readonly IWidgetManagerService _widgetManagerService = widgetManagerService;
    private readonly IWidgetResourceService _widgetResourceService = widgetResourceService;
    // TODO: Move to ActualTheme.
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
                await _microsoftWidgetModel.AddWidgetsAsync(newWidgetDefinition1, () => DialogFactory.ShowCreateWidgetErrorDialogAsync(), async (wvm) =>
                {
                    await _widgetManagerService.AddWidgetAsync(wvm, RefreshAddedWidgetAsync, false);
                });
            }
        }
    }

    #endregion

    #region Load

    private async Task LoadYourWidgetsAsync()
    {
        yourWidgets = await _widgetResourceService.GetYourDashboardWidgetItemsAsync(_themeSelectorService.GetActualTheme());
        foreach (var item in yourWidgets)
        {
            item.PinnedChangedCallback = OnPinnedChanged;
        }
    }

    private async void OnPinnedChanged(DashboardWidgetItem item)
    {
        if (item.Pinned)
        {
            await _widgetManagerService.PinWidgetAsync(item.ProviderType, item.Id, item.Type, item.Index, false);
        }
        else
        {
            await _widgetManagerService.UnpinWidgetAsync(item.ProviderType, item.Id, item.Type, item.Index, false);
        }

        var index = yourWidgets.FindIndex(x => x.Equals(item.ProviderType, item.Id, item.Type, item.Index));
        if (index != -1)
        {
            yourWidgets[index].Pinned = item.Pinned;

            RefreshYourWidgets();
        }
    }

    #endregion

    #region Refresh

    internal void RefreshDeletedWidget(WidgetProviderType providerType, string widgetId, string widgetType, int widgetIndex)
    {
        var index = yourWidgets.FindIndex(x => x.Equals(providerType, widgetId, widgetType, widgetIndex));
        if (index != -1)
        {
            yourWidgets.RemoveAt(index);

            RefreshYourWidgets();
        }
    }

    private async Task RefreshAddedWidgetAsync(string widgetId, string widgetType, int widgetIndex)
    {
        var widgetItem = await _widgetResourceService.GetDashboardWidgetItemAsync(widgetId, widgetType, widgetIndex, _themeSelectorService.GetActualTheme());
        if (widgetItem != null)
        {
            widgetItem.PinnedChangedCallback = OnPinnedChanged;
            yourWidgets.Add(widgetItem);

            RefreshYourWidgets();
        }
    }

    // TODO: Improve code quality to unifying function like this.
    private async Task RefreshAddedWidgetAsync(WidgetViewModel widgetViewModel)
    {
        var widgetItem = await _widgetResourceService.GetDashboardWidgetItemAsync(widgetViewModel, _themeSelectorService.GetActualTheme());
        if (widgetItem != null)
        {
            widgetItem.PinnedChangedCallback = OnPinnedChanged;
            yourWidgets.Add(widgetItem);

            RefreshYourWidgets();
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

    public async void OnNavigatedTo(object parameter)
    {
        if (!_isInitialized)
        {
            await LoadYourWidgetsAsync();
            RefreshYourWidgets();

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
                case DashboardViewModelNavigationParameter.UpdateEvent.Pin:
                    if (providerType == WidgetProviderType.DesktopWidgets3)
                    {
                        var widgetItem = await _widgetResourceService.GetDashboardWidgetItemAsync(widgetId, widgetType, widgetIndex, actualTheme);
                        if (widgetItem != null)
                        {
                            widgetItem.PinnedChangedCallback = OnPinnedChanged;
                            yourWidgets.Add(widgetItem);

                            RefreshYourWidgets();
                        }
                    }
                    else
                    {
                        var widgetViewModel = _widgetManagerService.GetWidgetViewModel(providerType, widgetId, widgetType, widgetIndex);
                        if (widgetViewModel != null)
                        {
                            var widgetItem = await _widgetResourceService.GetDashboardWidgetItemAsync(widgetViewModel, actualTheme);
                            if (widgetItem != null)
                            {
                                widgetItem.PinnedChangedCallback = OnPinnedChanged;
                                yourWidgets.Add(widgetItem);

                                RefreshYourWidgets();
                            }
                        }
                    }
                    break;
                case DashboardViewModelNavigationParameter.UpdateEvent.Unpin:
                    var index = yourWidgets.FindIndex(x => x.Equals(providerType, widgetId, widgetType, widgetIndex));
                    if (index != -1)
                    {
                        yourWidgets[index].Pinned = false;
                        yourWidgets[index].PinnedChangedCallback = OnPinnedChanged;

                        RefreshYourWidgets();
                    }
                    break;
                case DashboardViewModelNavigationParameter.UpdateEvent.Delete:
                    var index1 = yourWidgets.FindIndex(x => x.Equals(providerType, widgetId, widgetType, widgetIndex));
                    if (index1 != -1)
                    {
                        yourWidgets.RemoveAt(index1);

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

    #region Theme Change

    public async Task UpdateThemeAsync(ElementTheme actualTheme)
    {
        foreach (var widgetItem in yourWidgets)
        {
            if (widgetItem.ProviderType == WidgetProviderType.DesktopWidgets3)
            {
                widgetItem.IconFill = await _widgetResourceService.GetWidgetIconBrushAsync(_dispatcherQueue, widgetItem.Id, widgetItem.Type, actualTheme);
            }
            else
            {
                // TODO: Add support.
                /*widgetItem.IconFill = await _widgetResourceService.GetWidgetIconBrushAsync(_dispatcherQueue, widgetItem.Id, widgetItem.Type, actualTheme);*/
            }
        }
    }

    #endregion
}
