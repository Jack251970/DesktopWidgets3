// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Collections.Specialized;
//using DevHome.Dashboard.Common.Contracts;
//using DevHome.Dashboard.Common.Extensions;
//using DevHome.Dashboard.Common.Helpers;
//using DevHome.Dashboard.Common.Services;
//using DevHome.Dashboard.Common.Views;
using DevHome.Dashboard.ComSafeWidgetObjects;
using DevHome.Dashboard.Helpers;
using DevHome.Dashboard.Services;
using DevHome.Dashboard.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.Widgets;
using Microsoft.Windows.Widgets.Hosts;
using Serilog;
using Windows.Foundation;

namespace DevHome.Dashboard.Models;

/// <summary>
/// Resource model and management model for microsoft widgets.
/// Edited from DashboardView.xaml.cs.
/// </summary>
public partial class MicrosoftWidgetModel : IDisposable
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(MicrosoftWidgetModel));

    public DashboardViewModel ViewModel { get; }

    public ObservableCollection<WidgetProviderDefinition> WidgetProviderDefinitions { get; set; } = [];
    public ObservableCollection<ComSafeWidgetDefinition> WidgetDefinitions { get; set; } = [];

    private readonly DispatcherQueue _dispatcherQueue;
    private readonly WidgetViewModelFactory _widgetViewModelFactory;
    private readonly IWidgetExtensionService _widgetExtensionService;

    private Func<WidgetViewModel, Task>? CreateWidgetWindow;

    private readonly SemaphoreSlim _pinnedWidgetsLock = new(1, 1);

    private readonly CancellationTokenSource _initWidgetsCancellationTokenSource = new();

    public MicrosoftWidgetModel()
    {
        ViewModel = DependencyExtensions.GetRequiredService<DashboardViewModel>();

        _dispatcherQueue = DependencyExtensions.GetRequiredService<DispatcherQueue>();
        _widgetViewModelFactory = DependencyExtensions.GetRequiredService<WidgetViewModelFactory>();
        _widgetExtensionService = DependencyExtensions.GetRequiredService<IWidgetExtensionService>();
    }

    #region Initialization

    public async Task InitializeResourcesAsync()
    {
        // Show the providers and widgets underneath them in alphabetical order
        var providerDefinitions = (await ViewModel.WidgetHostingService.GetProviderDefinitionsAsync()).OrderBy(x => x.DisplayName);
        var comSafeWidgetDefinitions = await ComSafeHelpers.GetAllOrderedComSafeWidgetDefinitions(ViewModel.WidgetHostingService);

        _log.Information($"Filling available widget list, found {providerDefinitions.Count()} providers and {comSafeWidgetDefinitions.Count} widgets");

        // Update the collections
        WidgetProviderDefinitions = new ObservableCollection<WidgetProviderDefinition>(providerDefinitions);
        WidgetDefinitions = new ObservableCollection<ComSafeWidgetDefinition>(comSafeWidgetDefinitions);
    }

    public async Task InitializePinnedWidgetsAsync(Func<WidgetViewModel, Task> createWidgetWindow)
    {
        CreateWidgetWindow = createWidgetWindow;
        await OnLoadedAsync();
    }

    #endregion

    #region Loaded & UnLoaded

    public async Task OnLoadedAsync()
    {
        ViewModel.IsLoading = true;
        ViewModel.HasWidgetServiceInitialized = false;

        if (await ValidateDashboardState())
        {
            ViewModel.HasWidgetServiceInitialized = true;
            await InitializeDashboard();
        }

        ViewModel.IsLoading = false;
    }

    private async Task InitializeDashboard()
    {
        try
        {
            await InitializePinnedWidgetListAsync(_initWidgetsCancellationTokenSource.Token);
            DependencyExtensions.GetRequiredService<IAdaptiveCardRenderingService>().RendererUpdated += HandleRendererUpdated;
        }
        catch (OperationCanceledException ex)
        {
            _log.Information(ex, "InitializePinnedWidgetListAsync operation was cancelled.");
            return;
        }
    }

    private async void HandleRendererUpdated(object? sender, object args)
    {
        // Re-render the widgets with the new theme and renderer.
        foreach (var widget in ViewModel.PinnedWidgets)
        {
            await widget.RenderAsync();
        }
    }

    private async Task InitializePinnedWidgetListAsync(CancellationToken cancellationToken)
    {
        var hostWidgets = await GetPreviouslyPinnedWidgets();
        await _pinnedWidgetsLock.WaitAsync(CancellationToken.None);
        try
        {
            await RestorePinnedWidgetsAsync(hostWidgets, cancellationToken);
        }
        finally
        {
            // No cleanup to do if the operation is cancelled.
            _pinnedWidgetsLock.Release();
        }
    }

    private async Task RestorePinnedWidgetsAsync(ComSafeWidget[] hostWidgets, CancellationToken cancellationToken)
    {
        var restoredWidgetsWithPosition = new SortedDictionary<int, ComSafeWidget>();
        var restoredWidgetsWithoutPosition = new SortedDictionary<int, ComSafeWidget>();
        var numUnorderedWidgets = 0;

        var pinnedSingleInstanceWidgets = new List<string>();

        _log.Information($"Restore pinned widgets");

        // Widgets do not come from the host in a deterministic order, so save their order in each widget's CustomState.
        // Iterate through all the widgets and put them in order. If a widget does not have a position assigned to it,
        // append it at the end. If a position is missing, just show the next widget in order.
        foreach (var widget in hostWidgets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var stateStr = await widget.GetCustomStateAsync();
                _log.Information($"GetWidgetCustomState: {stateStr}");

                if (string.IsNullOrEmpty(stateStr))
                {
                    // If we have a widget with no state, Dev Home does not consider it a valid widget
                    // and should delete it, rather than letting it run invisibly in the background.
                    await DeleteAbandonedWidgetAsync(widget);
                    continue;
                }

                var stateObj = System.Text.Json.JsonSerializer.Deserialize(stateStr, SourceGenerationContext.Default.WidgetCustomState);
                if (stateObj?.Host != WidgetHelpers.DevHomeHostName)
                {
                    // This shouldn't be able to be reached
                    _log.Error($"Widget has custom state but no HostName.");
                    continue;
                }

                var widgetDefinitionId = widget.DefinitionId;
                var unsafeWidgetDefinition = await ViewModel.WidgetHostingService.GetWidgetDefinitionAsync(widgetDefinitionId);
                if (unsafeWidgetDefinition == null)
                {
                    await DeleteWidgetWithNoDefinition(widget, widgetDefinitionId);
                    continue;
                }

                var comSafeWidgetDefinition = new ComSafeWidgetDefinition(widgetDefinitionId);
                if (!await comSafeWidgetDefinition.PopulateAsync())
                {
                    _log.Error($"Error populating widget definition for widget {widgetDefinitionId}");
                    await DeleteWidgetWithNoDefinition(widget, widgetDefinitionId);
                    continue;
                }

                // If the widget's extension was disabled, hide the widget (don't add it to the list), but don't delete it.
                if (!await WidgetHelpers.IsIncludedWidgetProviderAsync(comSafeWidgetDefinition.ProviderDefinition))
                {
                    _log.Information($"Not adding widget from disabled extension {comSafeWidgetDefinition.ProviderDefinitionId}");
                    continue;
                }

                // Ensure only one copy of a widget is pinned if that widget's definition only allows for one instance.
                if (comSafeWidgetDefinition.AllowMultiple == false)
                {
                    if (pinnedSingleInstanceWidgets.Contains(widgetDefinitionId))
                    {
                        _log.Information($"No longer allowed to have multiple of widget {widgetDefinitionId}");
                        await widget.DeleteAsync();
                        _log.Information($"Deleted Widget {widgetDefinitionId} and not adding it to PinnedWidgets");
                        continue;
                    }
                    else
                    {
                        pinnedSingleInstanceWidgets.Add(widgetDefinitionId);
                    }
                }

                var position = stateObj.Position;
                if (position >= 0)
                {
                    if (!restoredWidgetsWithPosition.TryAdd(position, widget))
                    {
                        // If there was an error and a widget with this position is already there,
                        // treat this widget as unordered and put it into the unordered map.
                        restoredWidgetsWithoutPosition.Add(numUnorderedWidgets++, widget);
                    }
                }
                else
                {
                    // Widgets with no position will get the default of -1. Append these at the end.
                    restoredWidgetsWithoutPosition.Add(numUnorderedWidgets++, widget);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"RestorePinnedWidgets(): ");
            }
        }

        // Merge the dictionaries for easier looping. restoredWidgetsWithoutPosition should be empty, so this should be fast.
        var lastOrderedKey = restoredWidgetsWithPosition.Count > 0 ? restoredWidgetsWithPosition.Last().Key : -1;
        restoredWidgetsWithoutPosition.ToList().ForEach(x => restoredWidgetsWithPosition.Add(++lastOrderedKey, x.Value));

        // Now that we've ordered the widgets, put them in their final collection.
        foreach (var orderedWidget in restoredWidgetsWithPosition)
        {
            var comSafeWidget = orderedWidget.Value;
            var size = await comSafeWidget.GetSizeAsync();
            cancellationToken.ThrowIfCancellationRequested();

            await InsertWidgetInPinnedWidgetsAsync(comSafeWidget, size, async (wvm) =>
            {
                if (CreateWidgetWindow != null)
                {
                    await CreateWidgetWindow.Invoke(wvm);
                }
            }, cancellationToken);
        }

        // Go through the newly created list of pinned widgets and update any positions that may have changed.
        // For example, if the provider for the widget at position 0 was deleted, the widget at position 1
        // should be updated to have position 0, etc.
        var updatedPlace = 0;
        foreach (var widget in ViewModel.PinnedWidgets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await WidgetHelpers.SetPositionCustomStateAsync(widget.Widget, updatedPlace++);
        }

        _log.Information($"Done restoring pinned widgets");
    }

    private async Task DeleteAbandonedWidgetAsync(ComSafeWidget widget)
    {
        var widgetList = await ViewModel.WidgetHostingService.GetWidgetsAsync();
        var length = widgetList.Length;
        _log.Information($"Found abandoned widget, try to delete it...");
        _log.Information($"Before delete, {length} widgets for this host");

        await widget.DeleteAsync();

        var newWidgetList = await ViewModel.WidgetHostingService.GetWidgetsAsync();
        length = newWidgetList.Length;
        _log.Information($"After delete, {length} widgets for this host");
    }

    private async Task<ComSafeWidget[]> GetPreviouslyPinnedWidgets()
    {
        _log.Information("Get widgets for current host");
        var unsafeHostWidgets = await ViewModel.WidgetHostingService.GetWidgetsAsync();
        if (unsafeHostWidgets.Length == 0)
        {
            _log.Information($"Found 0 widgets for this host");
            return [];
        }

        var comSafeHostWidgets = new List<ComSafeWidget>();
        foreach (var unsafeWidget in unsafeHostWidgets)
        {
            var id = await ComSafeWidget.GetIdFromUnsafeWidgetAsync(unsafeWidget);
            if (!string.IsNullOrEmpty(id))
            {
                var comSafeWidget = new ComSafeWidget(id);
                if (await comSafeWidget.PopulateAsync())
                {
                    comSafeHostWidgets.Add(comSafeWidget);
                }
            }
        }

        _log.Information($"Found {comSafeHostWidgets.Count} widgets for this host");

        return [.. comSafeHostWidgets];
    }

    private async Task<bool> ValidateDashboardState()
    {
        // Ensure we're not running elevated. Display an error and don't allow using the Dashboard if we are.
        if (ViewModel.IsRunningElevated())
        {
            _log.Error($"Dev Home is running as admin, can't show Dashboard");
            // TODO: Let user known this.
            // RunningAsAdminMessageStackPanel.Visibility = Visibility.Visible;
            return false;
        }

        // Ensure the WidgetService is installed and up to date.
        // TODO: Let user known this.
        /*var widgetServiceState = ViewModel.WidgetServiceService.GetWidgetServiceState();
        switch (widgetServiceState)
        {
            case WidgetServiceService.WidgetServiceStates.MeetsMinVersion:
                _log.Information($"WidgetServiceState meets min version");
                break;
            case WidgetServiceService.WidgetServiceStates.NotAtMinVersion:
                _log.Warning($"Initialization failed, WidgetServiceState not at min version");
                UpdateWidgetsMessageStackPanel.Visibility = Visibility.Visible;
                return false;
            case WidgetServiceService.WidgetServiceStates.NotOK:
                _log.Warning($"Initialization failed, WidgetServiceState not OK");
                NotOKServiceMessageStackPanel.Visibility = Visibility.Visible;
                return false;
            case WidgetServiceService.WidgetServiceStates.Updating:
                _log.Warning($"Initialization failed, WidgetServiceState updating");
                UpdatingWidgetServiceMessageStackPanel.Visibility = Visibility.Visible;
                return false;
            case WidgetServiceService.WidgetServiceStates.Unknown:
                _log.Error($"Initialization failed, WidgetServiceState unknown");
                RestartDevHomeMessageStackPanel.Visibility = Visibility.Visible;
                return false;
            default:
                break;
        }*/

        // Ensure we can access the WidgetService and subscribe to WidgetCatalog events.
        if (!await SubscribeToWidgetCatalogEventsAsync())
        {
            _log.Error($"Catalog event subscriptions failed, show error");
            // TODO: Let user known this.
            /*RestartDevHomeMessageStackPanel.Visibility = Visibility.Visible;*/
            return false;
        }

        return true;
    }

    private async Task<bool> SubscribeToWidgetCatalogEventsAsync()
    {
        _log.Information("SubscribeToWidgetCatalogEvents");

        try
        {
            var widgetCatalog = await ViewModel.WidgetHostingService.GetWidgetCatalogAsync();
            if (widgetCatalog == null)
            {
                _log.Error("Error in in SubscribeToWidgetCatalogEvents, widgetCatalog == null");
                return false;
            }

            widgetCatalog.WidgetDefinitionAdded += WidgetCatalog_WidgetDefinitionAdded;
            widgetCatalog.WidgetDefinitionDeleted += WidgetCatalog_WidgetDefinitionDeleted;
            widgetCatalog.WidgetDefinitionUpdated += WidgetCatalog_WidgetDefinitionUpdated;

            widgetCatalog.WidgetProviderDefinitionAdded += WidgetCatalog_WidgetProviderDefinitionAdded;
            widgetCatalog.WidgetProviderDefinitionDeleted += WidgetCatalog_WidgetProviderDefinitionDeleted;
            widgetCatalog.WidgetProviderDefinitionUpdated += WidgetCatalog_WidgetProviderDefinitionUpdated;
        }
        catch (Exception ex)
        {
            // If there was an error getting the widget catalog, log it and continue.
            // If a WidgetDefinition is deleted while the dialog is open, we won't know to remove it from
            // the list automatically, but we can show a helpful error message if the user tries to pin it.
            // https://github.com/microsoft/devhome/issues/2623
            _log.Error(ex, "Exception in SubscribeToWidgetCatalogEvents:");
            return false;
        }

        return true;
    }

    public async Task OnUnloadedAsync()
    {
        _log.Debug($"Unloading Dashboard, cancel any loading.");
        _initWidgetsCancellationTokenSource?.Cancel();
        ViewModel.PinnedWidgets.CollectionChanged -= OnPinnedWidgetsCollectionChangedAsync;
        // TODO: What is the function of it?
        // Bindings.StopTracking();

        DependencyExtensions.GetRequiredService<IAdaptiveCardRenderingService>().RendererUpdated -= HandleRendererUpdated;

        _log.Debug($"Leaving Dashboard, deactivating widgets.");

        await _pinnedWidgetsLock.WaitAsync();
        try
        {
            await Task.Run(UnsubscribeFromWidgets);
        }
        finally
        {
            ViewModel.PinnedWidgets.Clear();
            _pinnedWidgetsLock.Release();
        }

        await UnsubscribeFromWidgetCatalogEventsAsync();
    }

    private void UnsubscribeFromWidgets()
    {
        try
        {
            foreach (var widget in ViewModel.PinnedWidgets)
            {
                widget.UnsubscribeFromWidgetUpdates();
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Exception in UnsubscribeFromWidgets:");
        }
    }

    private async Task UnsubscribeFromWidgetCatalogEventsAsync()
    {
        _log.Information("UnsubscribeFromWidgetCatalogEvents");

        try
        {
            var widgetCatalog = await ViewModel.WidgetHostingService.GetWidgetCatalogAsync();
            if (widgetCatalog == null)
            {
                return;
            }

            widgetCatalog.WidgetDefinitionAdded -= WidgetCatalog_WidgetDefinitionAdded;
            widgetCatalog.WidgetDefinitionDeleted -= WidgetCatalog_WidgetDefinitionDeleted;
            widgetCatalog.WidgetDefinitionUpdated -= WidgetCatalog_WidgetDefinitionUpdated;

            widgetCatalog.WidgetProviderDefinitionAdded -= WidgetCatalog_WidgetProviderDefinitionAdded;
            widgetCatalog.WidgetProviderDefinitionDeleted -= WidgetCatalog_WidgetProviderDefinitionDeleted;
            widgetCatalog.WidgetProviderDefinitionUpdated -= WidgetCatalog_WidgetProviderDefinitionUpdated;
        }
        catch (Exception ex)
        {
            // If there was an error getting the widget catalog, log it and continue.
            _log.Error(ex, "Exception in UnsubscribeFromWidgetCatalogEventsAsync:");
        }
    }

    #endregion

    #region WidgetCatalog Events

    public event TypedEventHandler<WidgetCatalog, WidgetDefinitionAddedEventArgs>? WidgetDefinitionAdded;
    public event TypedEventHandler<WidgetCatalog, WidgetDefinitionDeletedEventArgs>? WidgetDefinitionDeleted;
    public event TypedEventHandler<WidgetCatalog, WidgetDefinitionUpdatedEventArgs>? WidgetDefinitionUpdated;

    public event TypedEventHandler<WidgetCatalog, WidgetProviderDefinitionAddedEventArgs>? WidgetProviderDefinitionAdded;
    public event TypedEventHandler<WidgetCatalog, WidgetProviderDefinitionDeletedEventArgs>? WidgetProviderDefinitionDeleted;
    public event TypedEventHandler<WidgetCatalog, WidgetProviderDefinitionUpdatedEventArgs>? WidgetProviderDefinitionUpdated;

    private void WidgetCatalog_WidgetDefinitionAdded(WidgetCatalog sender, WidgetDefinitionAddedEventArgs args)
    {
        WidgetDefinitionAdded?.Invoke(sender, args);
    }

    // Remove widget(s) from the Dashboard if the provider deletes the widget definition, or the provider is uninstalled.
    private void WidgetCatalog_WidgetDefinitionDeleted(WidgetCatalog sender, WidgetDefinitionDeletedEventArgs args)
    {
        WidgetDefinitionDeleted?.Invoke(sender, args);

        var definitionId = args.DefinitionId;
        _dispatcherQueue.TryEnqueue(async () =>
        {
            _log.Information($"WidgetDefinitionDeleted {definitionId}");
            foreach (var widgetToRemove in ViewModel.PinnedWidgets.Where(x => x.Widget.DefinitionId == definitionId).ToList())
            {
                _log.Information($"Remove widget {widgetToRemove.Widget.Id}");
                ViewModel.PinnedWidgets.Remove(widgetToRemove);

                // The widget definition is gone, so delete widgets with that definition.
                await widgetToRemove.Widget.DeleteAsync();
            }
        });

        ViewModel.WidgetIconService.RemoveIconsFromMicrosoftCache(definitionId);
        ViewModel.WidgetScreenshotService.RemoveScreenshotsFromMicrosoftCache(definitionId);
    }

    private async void WidgetCatalog_WidgetDefinitionUpdated(WidgetCatalog sender, WidgetDefinitionUpdatedEventArgs args)
    {
        WidgetDefinitionUpdated?.Invoke(sender, args);

        WidgetDefinition unsafeWidgetDefinition;
        try
        {
            unsafeWidgetDefinition = await Task.Run(() => args.Definition);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "WidgetCatalog_WidgetDefinitionUpdated: Couldn't get args.WidgetDefinition");
            return;
        }

        if (unsafeWidgetDefinition == null)
        {
            _log.Error("WidgetCatalog_WidgetDefinitionUpdated: Couldn't get WidgetDefinition");
            return;
        }

        var widgetDefinitionId = await ComSafeWidgetDefinition.GetIdFromUnsafeWidgetDefinitionAsync(unsafeWidgetDefinition);
        var comSafeNewDefinition = new ComSafeWidgetDefinition(widgetDefinitionId);
        if (!await comSafeNewDefinition.PopulateAsync())
        {
            _log.Error($"Error populating widget definition for widget {widgetDefinitionId}");
            return;
        }

        var updatedDefinitionId = comSafeNewDefinition.Id;
        _log.Information($"WidgetCatalog_WidgetDefinitionUpdated {updatedDefinitionId}");

        var matchingWidgetsFound = 0;

        foreach (var widgetToUpdate in ViewModel.PinnedWidgets.Where(x => x.Widget.DefinitionId == updatedDefinitionId).ToList())
        {
            // Things in the definition that we need to update to if they have changed:
            // AllowMultiple, DisplayTitle, Capabilities (size), ThemeResource (icons)
            var oldDef = widgetToUpdate.WidgetDefinition;

            // If we're no longer allowed to have multiple instances of this widget, delete all but the first.
            if (++matchingWidgetsFound > 1 && comSafeNewDefinition.AllowMultiple == false && oldDef.AllowMultiple == true)
            {
                _dispatcherQueue.TryEnqueue(async () =>
                {
                    _log.Information($"No longer allowed to have multiple of widget {updatedDefinitionId}");
                    _log.Information($"Delete widget {widgetToUpdate.Widget.Id}");
                    ViewModel.PinnedWidgets.Remove(widgetToUpdate);
                    await widgetToUpdate.Widget.DeleteAsync();
                    _log.Information($"Deleted Widget {widgetToUpdate.Widget.Id}");
                });
            }
            else
            {
                // Changing the definition updates the DisplayTitle.
                widgetToUpdate.WidgetDefinition = comSafeNewDefinition;

                // If the size the widget is currently set to is no longer supported by the widget, revert to its default size.
                // DevHomeTODO: Need to update WidgetControl with now-valid sizes.
                // DevHomeTODO: Properly compare widget capabilities.
                // https://github.com/microsoft/devhome/issues/641
                if (await oldDef.GetWidgetCapabilitiesAsync() != await comSafeNewDefinition.GetWidgetCapabilitiesAsync())
                {
                    // DevHomeTODO: handle the case where this change is made while Dev Home is not running -- how do we restore?
                    // https://github.com/microsoft/devhome/issues/641
                    if (!(await comSafeNewDefinition.GetWidgetCapabilitiesAsync()).Any(cap => cap.Size == widgetToUpdate.WidgetSize))
                    {
                        var newDefaultSize = WidgetHelpers.GetDefaultWidgetSize(await comSafeNewDefinition.GetWidgetCapabilitiesAsync());
                        widgetToUpdate.WidgetSize = newDefaultSize;
                        await widgetToUpdate.Widget.SetSizeAsync(newDefaultSize);
                    }
                }
            }

            // DevHomeTODO: ThemeResource (icons) changed.
            // https://github.com/microsoft/devhome/issues/641
        }
    }

    private void WidgetCatalog_WidgetProviderDefinitionAdded(WidgetCatalog sender, WidgetProviderDefinitionAddedEventArgs args)
    {
        WidgetProviderDefinitionAdded?.Invoke(sender, args);
    }

    private void WidgetCatalog_WidgetProviderDefinitionDeleted(WidgetCatalog sender, WidgetProviderDefinitionDeletedEventArgs args)
    {
        WidgetProviderDefinitionDeleted?.Invoke(sender, args);
    }

    private void WidgetCatalog_WidgetProviderDefinitionUpdated(WidgetCatalog sender, WidgetProviderDefinitionUpdatedEventArgs args)
    {
        WidgetProviderDefinitionUpdated?.Invoke(sender, args);
    }

    #endregion

    #region Store

    // TODO: Add support.
    public async Task GoToWidgetsInStoreAsync()
    {
        if (RuntimeHelper.IsOnWindows11)
        {
            await Windows.System.Launcher.LaunchUriAsync(new($"ms-windows-store://pdp/?productid={WidgetHelpers.WebExperiencePackPackageId}"));
        }
        else
        {
            await Windows.System.Launcher.LaunchUriAsync(new($"ms-windows-store://pdp/?productid={WidgetHelpers.WidgetsPlatformRuntimePackageId}"));
        }
    }

    #endregion

    #region Get Widgets

    public Task<Widget[]> GetWidgetsAsync()
    {
        return ViewModel.WidgetHostingService.GetWidgetsAsync();
    }

    #endregion

    #region Add Widget

    public async Task TryDeleteWidgetViewModel(WidgetViewModel widgetViewModel)
    {
        await TryDeleteUnsafeWidget(widgetViewModel.Widget.GetUnsafeWidgetObject());
    }

    public async Task AddWidgetsAsync(ComSafeWidgetDefinition newWidgetDefinition, Func<Task> showCreateErrorMessageAsync, Func<WidgetViewModel, Task> insertWidgetAsync)
    {
        try
        {
            var size = WidgetHelpers.GetDefaultWidgetSize(await newWidgetDefinition.GetWidgetCapabilitiesAsync());
            var unsafeWidget = await ViewModel.WidgetHostingService.CreateWidgetAsync(newWidgetDefinition.Id, size);
            if (unsafeWidget == null)
            {
                // Couldn't create the widget, show an error message.
                _log.Error($"Failure in CreateWidgetAsync, can't create the widget");
                await showCreateErrorMessageAsync();
                return;
            }

            var unsafeWidgetId = await ComSafeWidget.GetIdFromUnsafeWidgetAsync(unsafeWidget);
            if (unsafeWidgetId == string.Empty)
            {
                _log.Error($"Couldn't get Widget.Id, can't create the widget");
                await showCreateErrorMessageAsync();

                // If we created the widget but can't get a ComSafeWidget and show it, delete the widget.
                // We can try and catch silently, since the user already saw an error that the widget couldn't be created.
                await TryDeleteUnsafeWidget(unsafeWidget);
                return;
            }

            var comSafeWidget = new ComSafeWidget(unsafeWidgetId);
            if (!await comSafeWidget.PopulateAsync())
            {
                _log.Error($"Couldn't populate the ComSafeWidget, can't create the widget");
                await showCreateErrorMessageAsync();

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
            await InsertWidgetInPinnedWidgetsAsync(comSafeWidget, size, insertWidgetAsync);
        }
        catch (Exception ex)
        {
            _log.Warning(ex, $"Creating widget failed: ");
            await showCreateErrorMessageAsync();
        }
    }

    private async Task TryDeleteUnsafeWidget(Widget unsafeWidget)
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

    private async Task InsertWidgetInPinnedWidgetsAsync(ComSafeWidget widget, WidgetSize size, Func<WidgetViewModel, Task> insertWidgetAsync, CancellationToken cancellationToken = default)
    {
        await Task.Run(
            async () =>
            {
                var widgetDefinitionId = widget.DefinitionId;
                var widgetId = widget.Id;
                _log.Information($"Insert widget in pinned widgets, id = {widgetId}");

                var unsafeWidgetDefinition = await ViewModel.WidgetHostingService.GetWidgetDefinitionAsync(widgetDefinitionId);
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
                            await insertWidgetAsync(wvm);
                        }
                        catch (Exception ex)
                        {
                            // DevHomeTODO: Support concurrency in dashboard. Today concurrent async execution can cause insertion errors.
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

    private async Task DeleteWidgetWithNoDefinition(ComSafeWidget widget, string widgetDefinitionId)
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

    // If a widget is removed from the list, update the saved positions of the following widgets.
    // If not updated, widgets pinned later may be assigned the same position as existing widgets,
    // since the saved position may be greater than the number of pinned widgets.
    // Unsubscribe from this event during drag and drop, since the drop event takes care of re-numbering.
    // TODO: Check if we need this event and check ViewModel.PinnedWidgets.
    private async void OnPinnedWidgetsCollectionChangedAsync(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            await _pinnedWidgetsLock.WaitAsync();
            try
            {
                var removedIndex = e.OldStartingIndex;
                _log.Debug($"Removed widget at index {removedIndex}");
                for (var i = removedIndex; i < ViewModel.PinnedWidgets.Count; i++)
                {
                    _log.Debug($"Updating widget position for widget now at {i}");
                    var widgetToUpdate = ViewModel.PinnedWidgets.ElementAt(i);
                    await WidgetHelpers.SetPositionCustomStateAsync(widgetToUpdate.Widget, i);
                }
            }
            finally
            {
                _pinnedWidgetsLock.Release();
            }
        }
    }

    #region Dispose

    private bool _disposed;

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _pinnedWidgetsLock.Dispose();
            }

            _disposed = true;
        }
    }

    #endregion
}
