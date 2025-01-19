// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Text.Json;
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
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(MicrosoftWidgetModel));

    public DashboardViewModel ViewModel { get; }

    // TODO(Future): Use lock like _existedWidgetsLock to add multi-threading support.
    public ObservableCollection<WidgetProviderDefinition> WidgetProviderDefinitions { get; private set; } = [];
    public ObservableCollection<ComSafeWidgetDefinition> WidgetDefinitions { get; private set; } = [];

    public List<WidgetViewModel> ExistedWidgets { get; set; } = [];

    private readonly DispatcherQueue _dispatcherQueue;
    private readonly WidgetViewModelFactory _widgetViewModelFactory;

    private Func<WidgetViewModel, int, Task>? CreateWidgetWindow;

    public readonly SemaphoreSlim _existedWidgetsLock = new(1, 1);

    private readonly CancellationTokenSource _initWidgetsCancellationTokenSource = new();

    public MicrosoftWidgetModel()
    {
        ViewModel = DependencyExtensions.GetRequiredService<DashboardViewModel>();

        _dispatcherQueue = DependencyExtensions.GetRequiredService<DispatcherQueue>();
        _widgetViewModelFactory = DependencyExtensions.GetRequiredService<WidgetViewModelFactory>();
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

    public async Task InitializePinnedWidgetsAsync(Func<WidgetViewModel, int, Task> createWidgetWindow)
    {
        CreateWidgetWindow = createWidgetWindow;
        await OnLoadedAsync(true);
    }

    public async Task ReinitializePinnedWidgetsAsync()
    {
        await OnLoadedAsync(false);
    }

    public async Task ClosePinnedWidgetsAsync()
    {
        await OnUnloadedAsync();
    }

    #endregion

    #region Widget View Model

    public async Task<WidgetViewModel?> GetWidgetViewModel(string widgetId, string widgetType, int widgetIndex, CancellationToken cancellationToken = default)
    {
        var hostWidgets = await GetPreviouslyPinnedWidgets();
        await _existedWidgetsLock.WaitAsync(CancellationToken.None);
        try
        {
            foreach (var widget in hostWidgets)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var checkResult = await CheckWidgetAsync(widget);
                    if (checkResult == null)
                    {
                        continue;
                    }

                    var (stateObj, comSafeWidgetDefinition) = checkResult.Value;
                    var widgetIndex1 = stateObj.Position;
                    var (_, _, _, widgetId1, widgetType1) = comSafeWidgetDefinition.GetWidgetProviderAndWidgetInfo();
                    if (widgetId1 == widgetId && widgetType1 == widgetType && widgetIndex1 == widgetIndex)
                    {
                        var size = await widget.GetSizeAsync();
                        var widgetViewModel = await GetWidgetViewModel(widget, size, cancellationToken);
                        if (widgetViewModel != null)
                        {
                            ExistedWidgets.Add(widgetViewModel);
                            return widgetViewModel;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(ex, $"GetWidgetViewModel(): ");
                }
            }
        }
        finally
        {
            // No cleanup to do if the operation is cancelled.
            _existedWidgetsLock.Release();
        }

        return null;
    }

    #endregion

    #region Loaded & UnLoaded

    private async Task OnLoadedAsync(bool validate)
    {
        ViewModel.IsLoading = true;

        if (validate)
        {
            ViewModel.HasWidgetServiceInitialized = false;

            if (await ValidateDashboardState())
            {
                ViewModel.HasWidgetServiceInitialized = true;
                DependencyExtensions.GetRequiredService<IAdaptiveCardRenderingService>().RendererUpdated += HandleRendererUpdated;
                await InitializeDashboard();
            }
        }
        else if (ViewModel.HasWidgetServiceInitialized)
        {
            await InitializeDashboard();
        }
        else if (await ValidateDashboardState())
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
        foreach (var widget in ExistedWidgets)
        {
            await widget.RenderAsync();
        }
    }

    private async Task InitializePinnedWidgetListAsync(CancellationToken cancellationToken)
    {
        var hostWidgets = await GetPreviouslyPinnedWidgets();
        await _existedWidgetsLock.WaitAsync(CancellationToken.None);
        try
        {
            await RestorePinnedWidgetsAsync(hostWidgets, cancellationToken);
        }
        finally
        {
            // No cleanup to do if the operation is cancelled.
            _existedWidgetsLock.Release();
        }
    }

    private async Task RestorePinnedWidgetsAsync(ComSafeWidget[] hostWidgets, CancellationToken cancellationToken)
    {
        var restoredWidgetsWithPosition = new Dictionary<ComSafeWidget, int>();

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
                var checkResult = await CheckWidgetAsync(widget);
                if (checkResult == null)
                {
                    continue;
                }

                var widgetDefinitionId = widget.DefinitionId;
                var (stateObj, comSafeWidgetDefinition) = checkResult.Value;

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

                // We use position to get widget index info.
                var position = stateObj.Position;
                if (position >= 0)
                {
                    restoredWidgetsWithPosition.Add(widget, position);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"RestorePinnedWidgets(): ");
            }
        }

        // Now that we've ordered the widgets, put them in their final collection.
        foreach (var orderedWidget in restoredWidgetsWithPosition)
        {
            var comSafeWidget = orderedWidget.Key;
            var index = orderedWidget.Value;
            var size = await comSafeWidget.GetSizeAsync();
            cancellationToken.ThrowIfCancellationRequested();

            await InsertWidgetInPinnedWidgetsAsync(comSafeWidget, size, async (wvm) =>
            {
                if (CreateWidgetWindow != null)
                {
                    await CreateWidgetWindow.Invoke(wvm, index);
                    ExistedWidgets.Add(wvm);
                }
            }, cancellationToken);
        }

        _log.Information($"Done restoring pinned widgets");
    }

    private async Task<(WidgetCustomState, ComSafeWidgetDefinition)?> CheckWidgetAsync(ComSafeWidget widget)
    {
        var stateStr = await widget.GetCustomStateAsync();
        _log.Information($"GetWidgetCustomState: {stateStr}");

        if (string.IsNullOrEmpty(stateStr))
        {
            // If we have a widget with no state, Dev Home does not consider it a valid widget
            // and should delete it, rather than letting it run invisibly in the background.
            await DeleteAbandonedWidgetAsync(widget);
            return null;
        }

        var stateObj = JsonSerializer.Deserialize(stateStr, SourceGenerationContext.Default.WidgetCustomState);
        if (stateObj?.Host != WidgetHelpers.DevHomeHostName)
        {
            // This shouldn't be able to be reached
            _log.Error($"Widget has custom state but no HostName.");
            return null;
        }

        var widgetDefinitionId = widget.DefinitionId;
        var unsafeWidgetDefinition = await ViewModel.WidgetHostingService.GetWidgetDefinitionAsync(widgetDefinitionId);
        if (unsafeWidgetDefinition == null)
        {
            await DeleteWidgetWithNoDefinition(widget, widgetDefinitionId);
            return null;
        }

        var comSafeWidgetDefinition = new ComSafeWidgetDefinition(widgetDefinitionId);
        if (!await comSafeWidgetDefinition.PopulateAsync())
        {
            _log.Error($"Error populating widget definition for widget {widgetDefinitionId}");
            await DeleteWidgetWithNoDefinition(widget, widgetDefinitionId);
            return null;
        }

        // If the widget's extension was disabled, hide the widget (don't add it to the list), but don't delete it.
        if (!await WidgetHelpers.IsIncludedWidgetProviderAsync(comSafeWidgetDefinition.ProviderDefinition))
        {
            _log.Information($"Not adding widget from disabled extension {comSafeWidgetDefinition.ProviderDefinitionId}");
            return null;
        }

        return (stateObj, comSafeWidgetDefinition);
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

    // TODO(Future): Let the user know why the dashboard is not available and set false flag.
    private async Task<bool> ValidateDashboardState()
    {
        // Ensure we're not running elevated. Display an error and don't allow using the Dashboard if we are.
        if (ViewModel.IsRunningElevated())
        {
            _log.Error($"Dev Home is running as admin, can't show Dashboard");
            // RunningAsAdminMessageStackPanel.Visibility = Visibility.Visible;
            return false;
        }

        // TODO(Future): Add support for widget service.
        // Ensure the WidgetService is installed and up to date.
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

    private async Task OnUnloadedAsync()
    {
        _log.Debug($"Unloading Dashboard, cancel any loading.");
        _initWidgetsCancellationTokenSource?.Cancel();

        DependencyExtensions.GetRequiredService<IAdaptiveCardRenderingService>().RendererUpdated -= HandleRendererUpdated;

        _log.Debug($"Leaving Dashboard, deactivating widgets.");

        await _existedWidgetsLock.WaitAsync();
        try
        {
            await Task.Run(UnsubscribeFromWidgets);
        }
        finally
        {
            foreach (var widget in ExistedWidgets)
            {
                widget.Dispose();
            }
            ExistedWidgets.Clear();
            _existedWidgetsLock.Release();
        }

        await UnsubscribeFromWidgetCatalogEventsAsync();
    }

    private void UnsubscribeFromWidgets()
    {
        try
        {
            foreach (var widget in ExistedWidgets)
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

        // TODO(Future): Add support for widget deletion.
        /*var definitionId = args.DefinitionId;
        _dispatcherQueue.TryEnqueue(async () =>
        {
            _log.Information($"WidgetDefinitionDeleted {definitionId}");
            foreach (var widgetToRemove in ExistedWidgets.Where(x => x.Widget.DefinitionId == definitionId).ToList())
            {
                _log.Information($"Remove widget {widgetToRemove.Widget.Id}");
                widgetToRemove.Dispose();
                ExistedWidgets.Remove(widgetToRemove);

                // The widget definition is gone, so delete widgets with that definition.
                await widgetToRemove.Widget.DeleteAsync();
            }
        });

        ViewModel.WidgetIconService.RemoveIconsFromMicrosoftIconCache(definitionId);
        ViewModel.WidgetScreenshotService.RemoveScreenshotsFromMicrosoftIconCache(definitionId);*/
    }

    private async void WidgetCatalog_WidgetDefinitionUpdated(WidgetCatalog sender, WidgetDefinitionUpdatedEventArgs args)
    {
        WidgetDefinitionUpdated?.Invoke(sender, args);

        // TODO(Future): Add support for widget update.
        /*WidgetDefinition unsafeWidgetDefinition;
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

        foreach (var widgetToUpdate in ExistedWidgets.Where(x => x.Widget.DefinitionId == updatedDefinitionId).ToList())
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
                    widgetToUpdate.Dispose();
                    ExistedWidgets.Remove(widgetToUpdate);
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
        }*/
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

    // TODO(Future): Check if we need this function.
    public static async Task GoToWidgetsInStoreAsync()
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

    public async Task<ComSafeWidget[]> GetComSafeWidgetsAsync()
    {
        var unsafeCurrentlyPinnedWidgets = await ViewModel.WidgetHostingService.GetWidgetsAsync();
        var comSafeCurrentlyPinnedWidgets = new List<ComSafeWidget>();
        foreach (var unsafeWidget in unsafeCurrentlyPinnedWidgets)
        {
            var id = await ComSafeWidget.GetIdFromUnsafeWidgetAsync(unsafeWidget);
            if (!string.IsNullOrEmpty(id))
            {
                var comSafeWidget = new ComSafeWidget(id);
                if (await comSafeWidget.PopulateAsync())
                {
                    comSafeCurrentlyPinnedWidgets.Add(comSafeWidget);
                }
            }
        }

        return [.. comSafeCurrentlyPinnedWidgets];
    }

    #endregion

    #region Add Widget

    public async Task TryDeleteWidgetAsync(WidgetViewModel widgetViewModel)
    {
        await TryDeleteWidgetAsync(widgetViewModel.Widget);
    }

    private static async Task TryDeleteWidgetAsync(ComSafeWidget widgetToDelete)
    {
        await TryDeleteWidgetAsync(widgetToDelete.GetUnsafeWidgetObject());
    }

    public async Task AddWidgetsAsync(ComSafeWidgetDefinition newWidgetDefinition, Func<Task> showCreateErrorMessageAsync, Func<WidgetViewModel, Task<int>> insertWidgetAsync)
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
                await TryDeleteWidgetAsync(unsafeWidget);
                return;
            }

            var comSafeWidget = new ComSafeWidget(unsafeWidgetId);
            if (!await comSafeWidget.PopulateAsync())
            {
                _log.Error($"Couldn't populate the ComSafeWidget, can't create the widget");
                await showCreateErrorMessageAsync();

                // If we created the widget but can't get a ComSafeWidget and show it, delete the widget.
                // We can try and catch silently, since the user already saw an error that the widget couldn't be created.
                await TryDeleteWidgetAsync(unsafeWidget);
                return;
            }

            // Put new widget on the Dashboard.
            await InsertWidgetInPinnedWidgetsAsync(comSafeWidget, size, async (wvm) =>
            {
                // Set custom state on new widget.
                // We use position to store widget index info.
                var position = await insertWidgetAsync(wvm);
                var newCustomState = WidgetHelpers.CreateWidgetCustomState(position);
                _log.Debug($"SetCustomState: {newCustomState}");
                await comSafeWidget.SetCustomStateAsync(newCustomState);
            });
        }
        catch (Exception ex)
        {
            _log.Warning(ex, $"Creating widget failed: ");
            await showCreateErrorMessageAsync();
        }
    }

    private static async Task TryDeleteWidgetAsync(Widget widgetToDelete)
    {
        // Remove the widget from the list before deleting, otherwise the widget will
        // have changed and the collection won't be able to find it to remove it.
        var widgetIdToDelete = widgetToDelete.Id;
        _log.Debug($"User removed widget, delete widget {widgetIdToDelete}");
        try
        {
            await widgetToDelete.DeleteAsync();
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Error deleting widget {widgetIdToDelete}");
        }
    }

    private async Task InsertWidgetInPinnedWidgetsAsync(ComSafeWidget widget, WidgetSize size, Func<WidgetViewModel, Task> insertWidgetAsync, CancellationToken cancellationToken = default)
    {
        await Task.Run(
            async () =>
            {
                var wvm = await GetWidgetViewModel(widget, size, cancellationToken);
                if (wvm == null)
                {
                    return;
                }

                _log.Information($"Insert widget in pinned widgets, id = {widget.Id}");
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
            },
            cancellationToken);
    }

    private async Task<WidgetViewModel?> GetWidgetViewModel(ComSafeWidget widget, WidgetSize size, CancellationToken cancellationToken)
    {
        var widgetDefinitionId = widget.DefinitionId;
        var widgetId = widget.Id;

        var unsafeWidgetDefinition = await ViewModel.WidgetHostingService.GetWidgetDefinitionAsync(widgetDefinitionId);
        if (unsafeWidgetDefinition != null)
        {
            var comSafeWidgetDefinition = new ComSafeWidgetDefinition(widgetDefinitionId);
            if (!await comSafeWidgetDefinition.PopulateAsync())
            {
                _log.Error($"Error getting widget in pinned widgets, id = {widgetId}");
                await widget.DeleteAsync();
                return null;
            }

            /*// The WidgetService will start the widget provider, however Dev Home won't know about it and won't be
            // able to send disposed events when Dev Home closes. Ensure the provider is started here so we can
            // tell the extension to dispose later.
            if (_widgetExtensionService.IsCoreWidgetProvider(comSafeWidgetDefinition.ProviderDefinitionId))
            {
                await _widgetExtensionService.EnsureCoreWidgetExtensionStarted(comSafeWidgetDefinition.ProviderDefinitionId);
            }*/

            var wvm = _widgetViewModelFactory(widget, size, comSafeWidgetDefinition);
            cancellationToken.ThrowIfCancellationRequested();
            return wvm;
        }
        else
        {
            await DeleteWidgetWithNoDefinition(widget, widgetDefinitionId);
            return null;
        }
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
                _existedWidgetsLock.Dispose();

                foreach (var widget in ExistedWidgets)
                {
                    widget.Dispose();
                }
                foreach (var widget in WidgetDefinitions)
                {
                    widget.Dispose();
                }

                ExistedWidgets.Clear();
                WidgetProviderDefinitions.Clear();
                WidgetDefinitions.Clear();
            }

            _disposed = true;
        }
    }

    #endregion
}
