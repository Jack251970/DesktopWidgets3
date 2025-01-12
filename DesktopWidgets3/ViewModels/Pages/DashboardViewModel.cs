using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.Widgets;
using Serilog;
using System.Collections.ObjectModel;

namespace DesktopWidgets3.ViewModels.Pages;

public partial class DashboardViewModel(DispatcherQueue dispatcherQueue, WidgetViewModelFactory widgetViewModelFactory, IWidgetExtensionService widgetExtensionService, IWidgetHostingService widgetHostingService, IWidgetManagerService widgetManagerService, IWidgetResourceService widgetResourceService) : ObservableRecipient, INavigationAware
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(DashboardViewModel));

    public ObservableCollection<DashboardWidgetItem> PinnedWidgets { get; set; } = [];
    public ObservableCollection<DashboardWidgetItem> UnpinnedWidgets { get; set; } = [];

    private readonly DispatcherQueue _dispatcherQueue = dispatcherQueue;

    private readonly WidgetViewModelFactory _widgetViewModelFactory = widgetViewModelFactory;

    private readonly IWidgetExtensionService _widgetExtensionService = widgetExtensionService;
    private readonly IWidgetHostingService _widgetHostingService = widgetHostingService;
    private readonly IWidgetManagerService _widgetManagerService = widgetManagerService;
    private readonly IWidgetResourceService _widgetResourceService = widgetResourceService;

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
                await _widgetManagerService.AddWidgetAsync(newWidgetDefinition.WidgetId, newWidgetDefinition.WidgetType, RefreshAddedWidget, false);
            }
            else if (newWidget is ComSafeWidgetDefinition newWidgetDefinition1)
            {
                try
                {
                    var size = WidgetHelpers.GetDefaultWidgetSize(await newWidgetDefinition1.GetWidgetCapabilitiesAsync());
                    var unsafeWidget = await _widgetHostingService.CreateWidgetAsync(newWidgetDefinition1.Id, size);
                    if (unsafeWidget == null)
                    {
                        // Couldn't create the widget, show an error message.
                        _log.Error($"Failure in CreateWidgetAsync, can't create the widget");
                        // TODO: Show error message.
                        // await ShowCreateWidgetErrorMessage();
                        return;
                    }

                    var unsafeWidgetId = await ComSafeWidget.GetIdFromUnsafeWidgetAsync(unsafeWidget);
                    if (unsafeWidgetId == string.Empty)
                    {
                        _log.Error($"Couldn't get Widget.Id, can't create the widget");
                        // TODO: Show error message.
                        // await ShowCreateWidgetErrorMessage();

                        // If we created the widget but can't get a ComSafeWidget and show it, delete the widget.
                        // We can try and catch silently, since the user already saw an error that the widget couldn't be created.
                        await TryDeleteUnsafeWidget(unsafeWidget);
                        return;
                    }

                    var comSafeWidget = new ComSafeWidget(unsafeWidgetId);
                    if (!await comSafeWidget.PopulateAsync())
                    {
                        _log.Error($"Couldn't populate the ComSafeWidget, can't create the widget");
                        // TODO: Show error message.
                        // await ShowCreateWidgetErrorMessage();

                        // If we created the widget but can't get a ComSafeWidget and show it, delete the widget.
                        // We can try and catch silently, since the user already saw an error that the widget couldn't be created.
                        await TryDeleteUnsafeWidget(unsafeWidget);
                        return;
                    }

                    // Set custom state on new widget.
                    // TODO: Remove position.
                    var position = -1;
                    var newCustomState = WidgetHelpers.CreateWidgetCustomState(position);
                    _log.Debug($"SetCustomState: {newCustomState}");
                    await comSafeWidget.SetCustomStateAsync(newCustomState);

                    // Put new widget on the Dashboard.
                    await InsertWidgetInPinnedWidgetsAsync(comSafeWidget, size);
                }
                catch (Exception ex)
                {
                    _log.Warning(ex, $"Creating widget failed: ");
                    // TODO: Show error message.
                    // await ShowCreateWidgetErrorMessage();
                }
            }
        }
    }

    private static async Task TryDeleteUnsafeWidget(Microsoft.Windows.Widgets.Hosts.Widget unsafeWidget)
    {
        try
        {
            await unsafeWidget.DeleteAsync();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error deleting widget");
        }
    }

    private async Task InsertWidgetInPinnedWidgetsAsync(ComSafeWidget widget, WidgetSize size, CancellationToken cancellationToken = default)
    {
        await Task.Run(
            async () =>
            {
                var widgetDefinitionId = widget.DefinitionId;
                var widgetId = widget.Id;
                _log.Information($"Insert widget in pinned widgets, id = {widgetId}");

                var unsafeWidgetDefinition = await _widgetHostingService.GetWidgetDefinitionAsync(widgetDefinitionId);
                if (unsafeWidgetDefinition != null)
                {
                    var comSafeWidgetDefinition = new ComSafeWidgetDefinition(widgetDefinitionId);
                    if (!await comSafeWidgetDefinition.PopulateAsync())
                    {
                        _log.Error($"Error inserting widget in pinned widgets, id = {widgetId}");
                        await widget.DeleteAsync();
                        return;
                    }

                    // The WidgetService will start the widget provider, however Dev Home won't know about it and won't be
                    // able to send disposed events when Dev Home closes. Ensure the provider is started here so we can
                    // tell the extension to dispose later.
                    if (_widgetExtensionService.IsCoreWidgetProvider(comSafeWidgetDefinition.ProviderDefinitionId))
                    {
                        await _widgetExtensionService.EnsureCoreWidgetExtensionStarted(comSafeWidgetDefinition.ProviderDefinitionId);
                    }

                    var wvm = _widgetViewModelFactory(widget, size, comSafeWidgetDefinition);
                    cancellationToken.ThrowIfCancellationRequested();

                    _dispatcherQueue.TryEnqueue(async () =>
                    {
                        try
                        {
                            //await _widgetManagerService.AddWidgetAsync(wvm);
                        }
                        catch (Exception ex)
                        {
                            // DEVHOMETODO: Support concurrency in dashboard. Today concurrent async execution can cause insertion errors.
                            // https://github.com/microsoft/devhome/issues/1215
                            _log.Warning(ex, $"Couldn't insert pinned widget");
                        }
                    });
                }
                else
                {
                    await DeleteWidgetWithNoDefinition(widget, widgetDefinitionId);
                }
            },
            cancellationToken);
    }

    private static async Task DeleteWidgetWithNoDefinition(ComSafeWidget widget, string widgetDefinitionId)
    {
        // If the widget provider was uninstalled while we weren't running, the catalog won't have the definition so delete the widget.
        _log.Information($"No widget definition '{widgetDefinitionId}', delete widget with that definition");
        try
        {
            await widget.SetCustomStateAsync(string.Empty);
            await widget.DeleteAsync();
        }
        catch (Exception ex)
        {
            _log.Information(ex, $"Error deleting widget");
        }
    }

    #endregion

    #region Load

    private void LoadYourWidgets()
    {
        yourWidgets = _widgetResourceService.GetYourDashboardWidgetItems();
        foreach (var item in yourWidgets)
        {
            item.PinnedChangedCallback = OnPinnedChanged;
        }
    }

    private async void OnPinnedChanged(DashboardWidgetItem item)
    {
        if (item.Pinned)
        {
            await _widgetManagerService.PinWidgetAsync(item.Id, item.Type, item.Index, false);
        }
        else
        {
            await _widgetManagerService.UnpinWidgetAsync(item.Id, item.Type, item.Index, false);
        }

        var index = yourWidgets.FindIndex(x => x.Id == item.Id & x.Type == item.Type & x.Index == item.Index);
        if (index != -1)
        {
            yourWidgets[index].Pinned = item.Pinned;

            RefreshYourWidgets();
        }
    }

    #endregion

    #region Refresh

    internal void RefreshUnpinnedWidget(string widgetId, string widgetType, int widgetIndex)
    {
        var index = yourWidgets.FindIndex(x => x.Id == widgetId & x.Type == widgetType & x.Index == widgetIndex);
        if (index != -1)
        {
            yourWidgets.RemoveAt(index);

            RefreshYourWidgets();
        }
    }

    private void RefreshAddedWidget(string widgetId, string widgetType, int widgetIndex)
    {
        var widgetItem = _widgetResourceService.GetDashboardWidgetItem(widgetId, widgetType, widgetIndex);
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

    public void OnNavigatedTo(object parameter)
    {
        if (!_isInitialized)
        {
            LoadYourWidgets();
            RefreshYourWidgets();

            _isInitialized = true;

            return;
        }

        if (parameter is DashboardViewModelNavigationParameter navigationParameter)
        {
            var widgetId = navigationParameter.Id;
            var widgetType = navigationParameter.Type;
            var widgetIndex = navigationParameter.Index;
            switch (navigationParameter.Event)
            {
                case DashboardViewModelNavigationParameter.UpdateEvent.Pin:
                    var widgetItem = _widgetResourceService.GetDashboardWidgetItem(widgetId, widgetType, widgetIndex);
                    if (widgetItem != null)
                    {
                        widgetItem.PinnedChangedCallback = OnPinnedChanged;
                        yourWidgets.Add(widgetItem);

                        RefreshYourWidgets();
                    }
                    break;
                case DashboardViewModelNavigationParameter.UpdateEvent.Unpin:
                    var index = yourWidgets.FindIndex(x => x.Id == widgetId & x.Type == widgetType & x.Index == widgetIndex);
                    if (index != -1)
                    {
                        yourWidgets[index].Pinned = false;
                        yourWidgets[index].PinnedChangedCallback = OnPinnedChanged;

                        RefreshYourWidgets();
                    }
                    break;
                case DashboardViewModelNavigationParameter.UpdateEvent.Delete:
                    var index1 = yourWidgets.FindIndex(x => x.Id == widgetId & x.Type == widgetType & x.Index == widgetIndex);
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
}
