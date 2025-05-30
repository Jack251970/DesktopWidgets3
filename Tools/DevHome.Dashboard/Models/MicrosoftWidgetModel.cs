﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using DevHome.Dashboard.Common.Services;
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
public partial class MicrosoftWidgetModel(DispatcherQueue dispatcherQueue, WidgetViewModelFactory widgetViewModelFactory, IExtensionService extensionService, IWidgetHostingService widgetHostingService, IWidgetServiceService widgetServiceService) : IDisposable
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(MicrosoftWidgetModel));

    public bool HasWidgetServiceInitialized { get; private set; } = false;

    public List<IExtensionWrapper> InstalledExtensions { get; set; } = [];

    public List<WidgetProviderDefinition> WidgetProviderDefinitions { get; private set; } = [];
    public List<ComSafeWidgetDefinition> WidgetDefinitions { get; private set; } = [];

    private readonly List<WidgetViewModel> ExistedWidgets = [];

    private readonly DispatcherQueue _dispatcherQueue = dispatcherQueue;
    private readonly WidgetViewModelFactory _widgetViewModelFactory = widgetViewModelFactory;
    private readonly IExtensionService _extensionService = extensionService;
    private readonly IWidgetHostingService _widgetHostingService = widgetHostingService;
    private readonly IWidgetServiceService _widgetServiceService = widgetServiceService;

    private Func<WidgetViewModel, int, Task<bool>>? CreateWidgetWindow;

    private readonly SemaphoreSlim _existedWidgetsLock = new(1, 1);

    public CancellationTokenSource InitWidgetsCancellationTokenSource { get; private set; } = null!;

    #region Initialization & Restart & Close

    public async Task InitializeResourcesAsync()
    {
        HasWidgetServiceInitialized = false;

        if (await ValidateDashboardState())
        {
            HasWidgetServiceInitialized = true;

            try
            {
                // Show the providers and widgets underneath them in alphabetical order
                var providerDefinitions = (await _widgetHostingService.GetProviderDefinitionsAsync()).OrderBy(x => x.DisplayName);
                var comSafeWidgetDefinitions = await ComSafeHelpers.GetAllOrderedComSafeWidgetDefinitions(_widgetHostingService);

                _log.Information($"Filling available Microsoft widget list, found {providerDefinitions.Count()} providers and {comSafeWidgetDefinitions.Count} widgets");

                // Update the collections
                WidgetProviderDefinitions = [.. providerDefinitions];
                WidgetDefinitions = comSafeWidgetDefinitions;

                // Initialize the extensions
                await InitializeExtensionsAsync();
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error initializing Microsoft widget resources");
            }
        }
    }

    public async Task InitializePinnedWidgetsAsync(Func<WidgetViewModel, int, Task<bool>> createWidgetWindow, CancellationTokenSource initWidgetsCancellationTokenSource)
    {
        if (!HasWidgetServiceInitialized)
        {
            // If the widget service is not initialized, don't try to initialize the pinned widgets.
            return;
        }

        _log.Information($"Initializing MicrosoftWidgetModel");

        CreateWidgetWindow = createWidgetWindow;
        InitWidgetsCancellationTokenSource = initWidgetsCancellationTokenSource;

        try
        {
            await OnLoadedAsync(true);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error initializing Microsoft widget pinned widgets");
        }

        _log.Debug($"MicrosoftWidgetModel initialized");
    }

    public async Task RestartPinnedWidgetsAsync()
    {
        if (!HasWidgetServiceInitialized)
        {
            // If the widget service is not initialized, don't try to restart the pinned widgets.
            return;
        }

        _log.Information($"Restarting MicrosoftWidgetModel");

        try
        {
            await OnLoadedAsync(false);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error restarting Microsoft widget pinned widgets");
        }

        _log.Debug($"MicrosoftWidgetModel restarted");
    }

    public async Task ClosePinnedWidgetsAsync()
    {
        if (!HasWidgetServiceInitialized)
        {
            // If the widget service is not initialized, don't need to close the pinned widgets.
            return;
        }

        _log.Information($"Closing MicrosoftWidgetModel");

        try
        {
            await OnUnloadedAsync();
            _extensionService.OnExtensionsChanged -= OnExtensionsChanged;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error closing Microsoft widget pinned widgets");
        }

        _log.Debug($"MicrosoftWidgetModel closed");
    }

    #endregion

    #region Packages / Provider / Extensions

    private async Task InitializeExtensionsAsync()
    {
        InstalledExtensions = await _extensionService.GetInstalledExtensionsAsync();

        _log.Information($"Found {InstalledExtensions.Count} installed extensions");

        _extensionService.OnExtensionsChanged += OnExtensionsChanged;
    }

    private void OnExtensionsChanged(object? sender, List<IExtensionWrapper> installedExtensions)
    {
        InstalledExtensions = installedExtensions;
    }

    #endregion

    #region Widget Definition

    public WidgetProviderDefinition? GetWidgetProviderDefinition(string widgetId)
    {
        if (!HasWidgetServiceInitialized)
        {
            // If the widget service is not initialized, return null
            return null;
        }

        foreach (var providerDefinition in WidgetProviderDefinitions)
        {
            var (_, widgetId1) = providerDefinition.GetWidgetProviderInfo();
            if (widgetId == widgetId1)
            {
                return providerDefinition;
            }
        }

        return null;
    }

    public ComSafeWidgetDefinition? GetWidgetDefinition(string widgetId, string widgetType)
    {
        if (!HasWidgetServiceInitialized)
        {
            // If the widget service is not initialized, return null
            return null;
        }

        foreach (var widgetDefinition in WidgetDefinitions)
        {
            var (_, _, _, widgetId1, widgetType1) = widgetDefinition.GetWidgetProviderAndWidgetInfo();
            if (widgetId == widgetId1 && widgetType == widgetType1)
            {
                return widgetDefinition;
            }
        }

        return null;
    }

    #endregion

    #region Widget View Model

    public async Task<WidgetViewModel?> GetWidgetViewModel(string widgetId, string widgetType, int widgetIndex)
    {
        if (!HasWidgetServiceInitialized)
        {
            // If the widget service is not initialized, return null
            return null;
        }

        var hostWidgets = await GetPreviouslyPinnedWidgets();
        await _existedWidgetsLock.WaitAsync(CancellationToken.None);
        try
        {
            foreach (var widget in hostWidgets)
            {
                InitWidgetsCancellationTokenSource.Token.ThrowIfCancellationRequested();

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
                        var widgetViewModel = await GetWidgetViewModel(widget, size, InitWidgetsCancellationTokenSource.Token);
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

    public async Task RemoveWidgetViewModelAsync(WidgetViewModel widgetViewModel)
    {
        await _existedWidgetsLock.WaitAsync();
        try
        {
            // Remove the widget from the list before deleting, otherwise the widget will
            // have changed and the collection won't be able to find it to remove it.
            widgetViewModel.Dispose();
            ExistedWidgets.Remove(widgetViewModel);
        }
        finally
        {
            _existedWidgetsLock.Release();
        }
    }

    #endregion

    #region Loaded & UnLoaded

    private async Task OnLoadedAsync(bool init)
    {
        if (init)
        {
            DependencyExtensions.GetRequiredService<IAdaptiveCardRenderingService>().RendererUpdated += HandleRendererUpdated;
        }
        
        await InitializeDashboard();
    }

    private async Task InitializeDashboard()
    {
        try
        {
            await InitializePinnedWidgetListAsync(InitWidgetsCancellationTokenSource.Token);
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
                    var addedWidget = await CreateWidgetWindow.Invoke(wvm, index);
                    if (addedWidget)
                    {
                        ExistedWidgets.Add(wvm);
                    }
                    else
                    {
                        wvm.Dispose();
                    }
                }
            }, cancellationToken);
        }

        _log.Information($"Done restoring pinned widgets: {restoredWidgetsWithPosition.Count}");
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
        if (stateObj?.Host != Constants.MicrosoftWidgetHostName)
        {
            // This shouldn't be able to be reached
            _log.Error($"Widget has custom state but no HostName.");
            return null;
        }

        var widgetDefinitionId = widget.DefinitionId;
        var unsafeWidgetDefinition = await _widgetHostingService.GetWidgetDefinitionAsync(widgetDefinitionId);
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
        if (!WidgetHelpers.IsIncludedWidgetProvider(comSafeWidgetDefinition.ProviderDefinition))
        {
            _log.Information($"Not adding widget from disabled extension {comSafeWidgetDefinition.ProviderDefinitionId}");
            return null;
        }

        return (stateObj, comSafeWidgetDefinition);
    }

    private async Task DeleteAbandonedWidgetAsync(ComSafeWidget widget)
    {
        var widgetList = await _widgetHostingService.GetWidgetsAsync();
        var length = widgetList.Length;

        _log.Information($"Found abandoned widget, try to delete it");
        _log.Information($"Before delete, {length} widgets for this host");

        await widget.DeleteAsync();

        var newWidgetList = await _widgetHostingService.GetWidgetsAsync();
        length = newWidgetList.Length;

        _log.Information($"After delete, {length} widgets for this host");
    }

    private async Task<ComSafeWidget[]> GetPreviouslyPinnedWidgets()
    {
        _log.Information("Get widgets for current host");
        var unsafeHostWidgets = await _widgetHostingService.GetWidgetsAsync();
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
        if (RuntimeHelper.IsCurrentProcessRunningElevated())
        {
            _log.Error($"Dev Home is running as admin, can't show Dashboard");
            await DialogFactory.ShowRunningAsAdminMessageDialogAsync();
            return false;
        }

        // Ensure the WidgetService is installed and up to date.
        var widgetServiceState = _widgetServiceService.GetWidgetServiceState();
        switch (widgetServiceState)
        {
            case WidgetServiceService.WidgetServiceStates.MeetsMinVersion:
                _log.Information($"WidgetServiceState meets min version");
                break;
            case WidgetServiceService.WidgetServiceStates.NotAtMinVersion:
                _log.Warning($"Initialization failed, WidgetServiceState not at min version");
                await DialogFactory.ShowUpdateWidgetsMessageDialogAsync();
                await GoToWidgetsInStoreAsync();
                return false;
            case WidgetServiceService.WidgetServiceStates.NotOK:
                _log.Warning($"Initialization failed, WidgetServiceState not OK");
                await DialogFactory.ShowNotOKServiceMessageDialogAsync();
                return false;
            case WidgetServiceService.WidgetServiceStates.Updating:
                _log.Warning($"Initialization failed, WidgetServiceState updating");
                await DialogFactory.ShowUpdatingWidgetServiceMessageDialogAsync();
                return false;
            case WidgetServiceService.WidgetServiceStates.Unknown:
                _log.Error($"Initialization failed, WidgetServiceState unknown");
                await DialogFactory.ShowRestartDevHomeMessageDialogAsync();
                return false;
            default:
                break;
        }

        // Ensure we can access the WidgetService and subscribe to WidgetCatalog events.
        if (!await SubscribeToWidgetCatalogEventsAsync())
        {
            _log.Error($"Catalog event subscriptions failed, show error");
            await DialogFactory.ShowRestartDevHomeMessageDialogAsync();
            return false;
        }

        return true;
    }

    private async Task<bool> SubscribeToWidgetCatalogEventsAsync()
    {
        _log.Information("SubscribeToWidgetCatalogEvents");

        try
        {
            var widgetCatalog = await _widgetHostingService.GetWidgetCatalogAsync();
            if (widgetCatalog == null)
            {
                _log.Error("Error in in SubscribeToWidgetCatalogEvents, widgetCatalog == null");
                return false;
            }

            widgetCatalog.WidgetDefinitionAdded += WidgetCatalog_WidgetDefinitionAdded;
            widgetCatalog.WidgetDefinitionDeleted += WidgetCatalog_WidgetDefinitionDeleted;
            widgetCatalog.WidgetDefinitionUpdated += WidgetCatalog_WidgetDefinitionUpdated;
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

        DependencyExtensions.GetRequiredService<IAdaptiveCardRenderingService>().RendererUpdated -= HandleRendererUpdated;

        _log.Debug($"Leaving Dashboard, deactivating widgets.");

        await _existedWidgetsLock.WaitAsync();
        try
        {
            _log.Debug($"Leaving Dashboard, unsubscribing from widgets.");

            await Task.Run(UnsubscribeFromWidgets);
        }
        finally
        {
            _log.Debug($"Leaving Dashboard, disposing widgets.");

            foreach (var widget in ExistedWidgets)
            {
                widget.Dispose();
            }
            ExistedWidgets.Clear();

            _existedWidgetsLock.Release();
        }

        _log.Debug($"Leaving Dashboard, unsubscribing from widget catalog events.");

        await UnsubscribeFromWidgetCatalogEventsAsync();
    }

    private void UnsubscribeFromWidgets()
    {
        try
        {
            _log.Debug($"UnsubscribeFromWidgets {ExistedWidgets.Count}");

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
            var widgetCatalog = await _widgetHostingService.GetWidgetCatalogAsync();
            if (widgetCatalog == null)
            {
                return;
            }

            widgetCatalog.WidgetDefinitionAdded -= WidgetCatalog_WidgetDefinitionAdded;
            widgetCatalog.WidgetDefinitionDeleted -= WidgetCatalog_WidgetDefinitionDeleted;
            widgetCatalog.WidgetDefinitionUpdated -= WidgetCatalog_WidgetDefinitionUpdated;
        }
        catch (Exception ex)
        {
            // If there was an error getting the widget catalog, log it and continue.
            _log.Error(ex, "Exception in UnsubscribeFromWidgetCatalogEventsAsync:");
        }
    }

    #endregion

    #region Widget Catalog Events

    public event TypedEventHandler<WidgetCatalog, WidgetDefinitionAddedEventArgs>? WidgetDefinitionAdded;
    public event TypedEventHandler<WidgetCatalog, WidgetDefinitionDeletedEventArgs>? WidgetDefinitionDeleted;
    public event TypedEventHandler<WidgetCatalog, WidgetDefinitionUpdatedEventArgs>? WidgetDefinitionUpdated;

    private async void WidgetCatalog_WidgetDefinitionAdded(WidgetCatalog sender, WidgetDefinitionAddedEventArgs args)
    {
        await Task.Run(OnWidgetDefinitionChange);

        WidgetDefinitionAdded?.Invoke(sender, args);
    }

    private async void WidgetCatalog_WidgetDefinitionDeleted(WidgetCatalog sender, WidgetDefinitionDeletedEventArgs args)
    {
        await Task.Run(OnWidgetDefinitionChange);

        WidgetDefinitionDeleted?.Invoke(sender, args);
    }

    private async void WidgetCatalog_WidgetDefinitionUpdated(WidgetCatalog sender, WidgetDefinitionUpdatedEventArgs args)
    {
        await Task.Run(OnWidgetDefinitionChange);

        WidgetDefinitionUpdated?.Invoke(sender, args);
    }

    private async Task OnWidgetDefinitionChange()
    {
        // Update the collections
        var providerDefinitions = (await _widgetHostingService.GetProviderDefinitionsAsync()).OrderBy(x => x.DisplayName);
        var comSafeWidgetDefinitions = await ComSafeHelpers.GetAllOrderedComSafeWidgetDefinitions(_widgetHostingService);

        WidgetProviderDefinitions = [.. providerDefinitions];
        WidgetDefinitions = comSafeWidgetDefinitions;
    }

    #endregion

    #region Store

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
        if (!HasWidgetServiceInitialized)
        {
            // If the widget service is not initialized, return an empty list
            return [];
        }

        var unsafeCurrentlyPinnedWidgets = await _widgetHostingService.GetWidgetsAsync();
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

    public static async Task TryDeleteWidgetAsync(WidgetViewModel widgetViewModel)
    {
        await TryDeleteWidgetAsync(widgetViewModel.Widget);
    }

    private static async Task TryDeleteWidgetAsync(ComSafeWidget widgetToDelete)
    {
        await TryDeleteWidgetAsync(widgetToDelete.GetUnsafeWidgetObject());
    }

    public async Task AddWidgetsAsync(ComSafeWidgetDefinition newWidgetDefinition, bool showErrorMessage, Func<WidgetViewModel, Task<int>> insertWidgetAsync)
    {
        try
        {
            var size = WidgetHelpers.GetDefaultWidgetSize(await newWidgetDefinition.GetWidgetCapabilitiesAsync());
            var unsafeWidget = await _widgetHostingService.CreateWidgetAsync(newWidgetDefinition.Id, size);
            if (unsafeWidget == null)
            {
                // Couldn't create the widget, show an error message.
                _log.Error($"Failure in CreateWidgetAsync, can't create the widget");
                if (showErrorMessage)
                {
                    await DialogFactory.ShowCreateWidgetErrorDialogAsync();
                }
                return;
            }

            var unsafeWidgetId = await ComSafeWidget.GetIdFromUnsafeWidgetAsync(unsafeWidget);
            if (unsafeWidgetId == string.Empty)
            {
                _log.Error($"Couldn't get Widget.Id, can't create the widget");
                if (showErrorMessage)
                {
                    await DialogFactory.ShowCreateWidgetErrorDialogAsync();
                }

                // If we created the widget but can't get a ComSafeWidget and show it, delete the widget.
                // We can try and catch silently, since the user already saw an error that the widget couldn't be created.
                await TryDeleteWidgetAsync(unsafeWidget);
                return;
            }

            var comSafeWidget = new ComSafeWidget(unsafeWidgetId);
            if (!await comSafeWidget.PopulateAsync())
            {
                _log.Error($"Couldn't populate the ComSafeWidget, can't create the widget");
                if (showErrorMessage)
                {
                    await DialogFactory.ShowCreateWidgetErrorDialogAsync();
                }

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
            if (showErrorMessage)
            {
                await DialogFactory.ShowCreateWidgetErrorDialogAsync();
            }
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

        var unsafeWidgetDefinition = await _widgetHostingService.GetWidgetDefinitionAsync(widgetDefinitionId);
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

                foreach (var widget in WidgetDefinitions)
                {
                    widget.Dispose();
                }

                foreach (var widget in ExistedWidgets)
                {
                    widget.Dispose();
                }

                WidgetDefinitionAdded = null;
                WidgetDefinitionDeleted = null;
                WidgetDefinitionUpdated = null;

                InstalledExtensions.Clear();
                WidgetProviderDefinitions.Clear();
                WidgetDefinitions.Clear();
                ExistedWidgets.Clear();
            }

            _disposed = true;
        }
    }

    #endregion
}
