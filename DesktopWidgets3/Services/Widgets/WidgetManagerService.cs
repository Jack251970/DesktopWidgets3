﻿using System.Collections.Concurrent;
using Microsoft.UI.Xaml;
using Microsoft.Windows.Widgets.Hosts;
using Serilog;
using Windows.Graphics;

namespace DesktopWidgets3.Services.Widgets;

internal partial class WidgetManagerService(MicrosoftWidgetModel microsoftWidgetModel, IActivationService activationService, IAppSettingsService appSettingsService, INavigationService navigationService, IWidgetResourceService widgetResourceService) : IWidgetManagerService
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(WidgetManagerService));

    private readonly MicrosoftWidgetModel _microsoftWidgetModel = microsoftWidgetModel;

    private readonly IActivationService _activationService = activationService;
    private readonly IAppSettingsService _appSettingsService = appSettingsService;
    private readonly INavigationService _navigationService = navigationService;
    private readonly IWidgetResourceService _widgetResourceService = widgetResourceService;

    private readonly ConcurrentDictionary<string, WidgetWindowPair> PinnedWidgetWindowPairs = [];
    private readonly ConcurrentDictionary<string, WidgetSettingPair> WidgetSettingPairs = [];

    private readonly Dictionary<Tuple<WidgetProviderType, string, string>, int> MatchingWidgetNumber = [];
    private readonly SemaphoreSlim _matchingWidgetNumberLock = new(1, 1);

    private readonly CancellationTokenSource _initWidgetsCancellationTokenSource = new();

    private readonly List<JsonWidgetItem> _originalWidgetList = [];
    private readonly SemaphoreSlim _editModeLock = new(1, 1);
    private bool _inEditMode = false;
    private bool _restoreMainWindow = false;

    #region Initialization & Restart & Close

    public async Task InitializePinnedWidgetsAsync(bool initialized)
    {
        _log.Information("Initializing pinned widgets (Initialized: {Initialized})", initialized);

        try
        {
            // initialize desktop widgets 3 widgets
            var providerType = WidgetProviderType.DesktopWidgets3;
            var pinnedDesktopWidgets3Widgets = _appSettingsService.GetWidgetsList().Where(x => x.ProviderType == providerType && x.Pinned).ToList();

            foreach (var widget in pinnedDesktopWidgets3Widgets)
            {
                _initWidgetsCancellationTokenSource.Token.ThrowIfCancellationRequested();

                // check single instance
                if (await IsWidgetSingleInstanceAndAlreadyPinnedAsync(widget.ProviderType, widget.Id, widget.Type))
                {
                    await _appSettingsService.UnpinWidgetAsync(widget.ProviderType, widget.Id, widget.Type, widget.Index);
                }
                else
                {
                    await CreateWidgetWindowAsync(widget);
                }
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error initializing Desktop Widgets 3 widgets");
        }

        _log.Debug("Desktop Widgets 3 widgets initialized");

        if (initialized)
        {
            var existedWidgets = new List<Tuple<WidgetProviderType, string, string, int>>();

            // initialize microsoft widgets
            await _microsoftWidgetModel.InitializePinnedWidgetsAsync((widget, index) =>
            {
                return CreateWidgetWindowAsync(index, widget, existedWidgets);
            }, _initWidgetsCancellationTokenSource);

            // We don't delete microsoft widget json items that aren't in the system widget storage but unpin them instead.
            // Because we need to keep the settings of the deleted widgets to restore them when the user re-adds them.
            var pinnedMicrosoftWidgets = _appSettingsService.GetWidgetsList().Where(x => x.ProviderType == WidgetProviderType.Microsoft && x.Pinned).ToList();
            foreach (var widget in pinnedMicrosoftWidgets)
            {
                if (existedWidgets.Contains(new(WidgetProviderType.Microsoft, widget.Id, widget.Type, widget.Index)))
                {
                    existedWidgets.Remove(new(WidgetProviderType.Microsoft, widget.Id, widget.Type, widget.Index));
                }
                else
                {
                    await _appSettingsService.UnpinWidgetAsync(widget.ProviderType, widget.Id, widget.Type, widget.Index);
                }
            }
        }
        else
        {
            // reinitalize microsoft widgets
            await _microsoftWidgetModel.RestartPinnedWidgetsAsync();
        }

        _log.Debug("Microsoft Widgets initialized");
    }

    private async Task<bool> CreateWidgetWindowAsync(int widgetIndex, WidgetViewModel widgetViewModel, List<Tuple<WidgetProviderType, string, string, int>> existedWidgets)
    {
        // get widget info
        var providerType = WidgetProviderType.Microsoft;
        var (_, widgetName, _, widgetId, widgetType) = widgetViewModel.GetWidgetProviderAndWidgetInfo();

        // find item
        var widgetList = _appSettingsService.GetWidgetsList();
        var item = widgetList.FirstOrDefault(x => x.Equals(providerType, widgetId, widgetType, widgetIndex));
        if (item != null)
        {
            if (item.Pinned)
            {
                // create widget window
                await CreateWidgetWindowAsync(item, widgetViewModel);
                existedWidgets.Add(new(providerType, widgetId, widgetType, widgetIndex));
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            // add widget
            var newItem = new JsonWidgetItem()
            {
                ProviderType = providerType,
                Name = widgetName,
                Id = widgetId,
                Type = widgetType,
                Index = widgetIndex,
                Pinned = true,
                Position = WidgetConstants.DefaultWidgetPosition,
                Size = _widgetResourceService.GetWidgetDefaultSize(widgetViewModel),
                DisplayMonitor = DisplayMonitor.GetPrimaryMonitorInfo(),
                Settings = new BaseWidgetSettings()
            };

            // create widget window
            await CreateWidgetWindowAsync(newItem, widgetViewModel);

            // save widget item
            await _appSettingsService.AddWidgetAsync(newItem);

            existedWidgets.Add(new(providerType, widgetId, widgetType, widgetIndex));
            return true;
        }
    }

    public async Task RestartAllWidgetsAsync()
    {
        _log.Debug("Restarting all widgets");

        try
        {
            // close all widgets
            await CloseAllWidgetWindowsAsync();

            // enable all enabled widgets
            await InitializePinnedWidgetsAsync(false);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error restarting all widgets");
        }

        _log.Debug("All widgets restarted");
    }

    public async Task CloseAllWidgetsAsync()
    {
        _log.Debug("Closing all widgets");

        try
        {
            // cancel initialization
            _initWidgetsCancellationTokenSource?.Cancel();

            // close desktop widgets 3 widgets
            await CloseAllWidgetWindowsAsync();

            // close microsoft widgets
            await _microsoftWidgetModel.ClosePinnedWidgetsAsync();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error closing all widgets");
        }

        _log.Debug("All widgets closed");
    }

    private async Task CloseAllWidgetWindowsAsync()
    {
        _log.Information("Closing all widget windows");

        // close all windows
        await GetPinnedWidgetWindows().EnqueueOrInvokeAsync(async (window) =>
        {
            await CloseWidgetWindowAsync(window.RuntimeId, CloseEvent.Unpin);
        }, Microsoft.UI.Dispatching.DispatcherQueuePriority.High);

        _log.Debug("All widget windows closed");
    }

    #endregion

    #region Widget Allow Multiple

    public event EventHandler? AllowMultipleWidgetChanged;

    public async Task<bool> IsWidgetSingleInstanceAndAlreadyPinnedAsync(WidgetProviderType providerType, string widgetId, string widgetType)
    {
        if (_widgetResourceService.GetWidgetAllowMultiple(providerType, widgetId, widgetType))
        {
            return false;
        }

        await _matchingWidgetNumberLock.WaitAsync();
        try
        {
            var key = new Tuple<WidgetProviderType, string, string>(providerType, widgetId, widgetType);
            if (MatchingWidgetNumber.TryGetValue(key, out var value))
            {
                return value > 0;
            }

            return false;
        }
        finally
        {
            _matchingWidgetNumberLock.Release();
        }
    }

    #endregion

    #region Widget Catalog Events

    private void InitializeWidgetCatalogEvents()
    {
        _microsoftWidgetModel.WidgetDefinitionDeleted += MicrosoftWidgetModel_WidgetDefinitionDeleted;
        _microsoftWidgetModel.WidgetDefinitionUpdated += MicrosoftWidgetModel_WidgetDefinitionUpdated;
    }

    // Remove widget(s) from the Dashboard if the provider deletes the widget definition, or the provider is uninstalled.
    private async void MicrosoftWidgetModel_WidgetDefinitionDeleted(WidgetCatalog sender, WidgetDefinitionDeletedEventArgs args)
    {
        var definitionId = args.DefinitionId;
        _log.Information($"WidgetDefinitionDeleted {definitionId}");

        var widgetsToRemove = GetWidgetsFromDefinitionId(definitionId);
        foreach (var widget in widgetsToRemove)
        {
            var widgetToRemove = widget.Item4;
            _log.Information($"Remove widget {widgetToRemove.Widget.Id}");
            await UnpinWidgetAsync(WidgetProviderType.Microsoft, widget.Item1, widget.Item2, widget.Item3, true);
        }

        DependencyExtensions.GetRequiredService<IWidgetIconService>().RemoveIconsFromMicrosoftIconCache(definitionId);
        DependencyExtensions.GetRequiredService<IWidgetScreenshotService>().RemoveScreenshotsFromMicrosoftIconCache(definitionId);
    }

    private async void MicrosoftWidgetModel_WidgetDefinitionUpdated(WidgetCatalog sender, WidgetDefinitionUpdatedEventArgs args)
    {
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

        var widgetsToUpdate = GetWidgetsFromDefinitionId(updatedDefinitionId);
        var matchingWidgetsFound = 0;
        foreach (var widget in widgetsToUpdate)
        {
            // Things in the definition that we need to update to if they have changed:
            // AllowMultiple, DisplayTitle, Capabilities (size), ThemeResource (icons)
            var widgetToUpdate = widget.Item4;
            var oldDef = widgetToUpdate.WidgetDefinition;

            // If we're no longer allowed to have multiple instances of this widget, delete all but the first.
            if (++matchingWidgetsFound > 1 && comSafeNewDefinition.AllowMultiple == false && oldDef.AllowMultiple == true)
            {
                _log.Information($"No longer allowed to have multiple of widget {updatedDefinitionId}");
                _log.Information($"Delete widget {widgetToUpdate.Widget.Id}");
                await UnpinWidgetAsync(WidgetProviderType.Microsoft, widget.Item1, widget.Item2, widget.Item3, true);
                _log.Information($"Deleted Widget {widgetToUpdate.Widget.Id}");
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
                    // We don't need to update widgets because we let the user freely change the widget size.
                    // if (!(await comSafeNewDefinition.GetWidgetCapabilitiesAsync()).Any(cap => cap.Size == widgetToUpdate.WidgetSize))
                    // {
                    //     var newDefaultSize = WidgetHelpers.GetDefaultWidgetSize(await comSafeNewDefinition.GetWidgetCapabilitiesAsync());
                    //     widgetToUpdate.WidgetSize = newDefaultSize;
                    //     await widgetToUpdate.Widget.SetSizeAsync(newDefaultSize);
                    //     await RestartWidgetAsync(WidgetProviderType.Microsoft, widget.Item1, widget.Item2, widget.Item3);
                    // }
                }
            }

            // DevHomeTODO: ThemeResource (icons) changed.
            // https://github.com/microsoft/devhome/issues/641
        }
    }

    private List<Tuple<string, string, int, WidgetViewModel>> GetWidgetsFromDefinitionId(string definitionId)
    {
        var widgetsToRemove = new List<Tuple<string, string, int, WidgetViewModel>>();
        foreach (var item in PinnedWidgetWindowPairs)
        {
            var pair = item.Value;
            var widgetViewModel = pair.Window.ViewModel.WidgetViewModel!;
            if (pair.ProviderType == WidgetProviderType.Microsoft && widgetViewModel.Widget.DefinitionId == definitionId)
            {
                widgetsToRemove.Add(new(pair.WidgetId, pair.WidgetType, pair.WidgetIndex, widgetViewModel));
            }
        }
        return widgetsToRemove;
    }

    #endregion

    #region Widget Info

    #region Runtime Id

    public (WidgetProviderType providerType, string widgetId, string widgetType, int widgetIndex) GetWidgetInfo(string widgetRuntimeId)
    {
        if (PinnedWidgetWindowPairs.TryGetValue(widgetRuntimeId, out var widgetWindowPairs))
        {
            return (widgetWindowPairs.ProviderType, widgetWindowPairs.WidgetId, widgetWindowPairs.WidgetType, widgetWindowPairs.WidgetIndex);
        }

        return (WidgetProviderType.DesktopWidgets3, string.Empty, string.Empty, -1);
    }

    private string GetWidgetRuntimeId(WidgetProviderType providerType, string widgetId, string widgetType, int widgetIndex)
    {
        foreach (var (widgetRuntimeId, widgetWindowPair) in PinnedWidgetWindowPairs)
        {
            if (widgetWindowPair.Equals(providerType, widgetId, widgetType, widgetIndex))
            {
                return widgetRuntimeId;
            }
        }

        return string.Empty;
    }

    #endregion

    #region Widget Info & Context

    public WidgetInfo? GetWidgetInfo(WidgetProviderType providerType, string widgetId, string widgetType, int widgetIndex)
    {
        // get widget window pair
        var widgetWindowPair = GetWidgetWindowPair(providerType, widgetId, widgetType, widgetIndex);

        // get widget info
        if (widgetWindowPair != null)
        {
            return widgetWindowPair.WidgetInfo;
        }

        return null;
    }

    public WidgetContext? GetWidgetContext(WidgetProviderType providerType, string widgetId, string widgetType, int widgetIndex)
    {
        // get widget info
        var widgetInfo = GetWidgetInfo(providerType, widgetId, widgetType, widgetIndex);

        // get widget context
        if (widgetInfo != null)
        {
            return (WidgetContext)widgetInfo.WidgetContext;
        }

        return null;
    }

    private WidgetWindowPair? GetWidgetWindowPair(WidgetProviderType providerType, string widgetId, string widgetType, int widgetIndex)
    {
        // get widget pair
        foreach (var widgetWindowPair in PinnedWidgetWindowPairs.Values)
        {
            if (widgetWindowPair.Equals(providerType, widgetId, widgetType, widgetIndex))
            {
                return widgetWindowPair;
            }
        }

        return null;
    }

    private WidgetWindowPair? GetWidgetWindowPair(string widgetRuntimeId)
    {
        // get widget pair
        if (widgetRuntimeId != string.Empty && PinnedWidgetWindowPairs.TryGetValue(widgetRuntimeId, out var value))
        {
            return value;
        }

        return null;
    }

    #endregion

    #region Is Active

    public bool GetWidgetIsActive(WidgetProviderType providerType, string widgetId, string widgetType, int widgetIndex)
    {
        // get widget window pair
        var widgetWindowPair = GetWidgetWindowPair(providerType, widgetId, widgetType, widgetIndex);

        // get widget is active
        if (widgetWindowPair != null)
        {
            return widgetWindowPair.Window.IsActive;
        }

        return false;
    }

    #endregion

    #endregion

    #region Widget Setting Info

    #region Setting Info & Runtime Id

    public (string widgetId, string widgetType, int widgetIndex) GetWidgetSettingInfo(string widgetSettingRuntimeId)
    {
        if (WidgetSettingPairs.TryGetValue(widgetSettingRuntimeId, out var widgetSettingPair))
        {
            return (widgetSettingPair.WidgetId, widgetSettingPair.WidgetType, widgetSettingPair.WidgetIndex);
        }

        return (string.Empty, string.Empty, -1);
    }

    private string GetWidgetSettingRuntimeId(string widgetId, string widgetType)
    {
        foreach (var (widgetSettingRuntimeId, widgetSettingPair) in WidgetSettingPairs)
        {
            if (widgetSettingPair.Equals(widgetId, widgetType))
            {
                return widgetSettingRuntimeId;
            }
        }

        return string.Empty;
    }

    #endregion

    #region Widget Setting Context

    public WidgetSettingContext? GetWidgetSettingContext(string widgetId, string widgetType)
    {
        // get widget setting pair
        var widgetSettingPair = GetWidgetSettingPair(widgetId, widgetType);

        // get widget setting context
        if (widgetSettingPair != null)
        {
            return widgetSettingPair.WidgetSettingContext;
        }

        return null;
    }

    private WidgetSettingPair? GetWidgetSettingPair(string widgetId, string widgetType)
    {
        // get widget setting runtime id
        var widgetSettingRuntimeId = GetWidgetSettingRuntimeId(widgetId, widgetType);

        // get widget setting pair
        return GetWidgetSettingPair(widgetSettingRuntimeId);
    }

    private WidgetSettingPair? GetWidgetSettingPair(string widgetSettingRuntimeId)
    {
        // get widget setting pair index
        if (widgetSettingRuntimeId != string.Empty && WidgetSettingPairs.TryGetValue(widgetSettingRuntimeId, out var value))
        {
            return value;
        }

        return null;
    }

    #endregion

    #endregion

    #region Widget View Model

    public WidgetViewModel? GetWidgetViewModel(string widgetId, string widgetType, int widgetIndex)
    {
        // get widget window pair
        var widgetWindowPair = GetWidgetWindowPair(WidgetProviderType.Microsoft, widgetId, widgetType, widgetIndex);

        // get widget view model
        if (widgetWindowPair != null)
        {
            return widgetWindowPair.Window.ViewModel.WidgetViewModel;
        }

        return null!;
    }

    #endregion

    #region Widget Window

    #region All Widgets

    private List<WidgetWindow> GetPinnedWidgetWindows()
    {
        var widgetWindows = new List<WidgetWindow>();
        foreach (var widgetWindowPair in PinnedWidgetWindowPairs.Values)
        {
            if (widgetWindowPair.Window != null)
            {
                widgetWindows.Add(widgetWindowPair.Window);
            }
        }
        return widgetWindows;
    }

    #endregion

    #region Single Widget

    #region Public

    public async Task AddWidgetAsync(string widgetId, string widgetType, Func<string, string, int, Task>? action, bool updateDashboard)
    {
        var widgetList = _appSettingsService.GetWidgetsList();

        // get widget info
        var providerType = WidgetProviderType.DesktopWidgets3;

        _log.Information("Add Desktop Widgets 3 widget (WidgetId: {WidgetId}, WidgetType: {WidgetType})", widgetId, widgetType);

        // find index tag
        var indexs = widgetList.Where(x => x.Equals(providerType, widgetId, widgetType)).Select(x => x.Index).ToList();
        indexs.Sort();
        var index = 0;
        foreach (var tag in indexs)
        {
            if (tag != index)
            {
                break;
            }
            index++;
        }

        // invoke action
        if (action != null)
        {
            await action(widgetId, widgetType, index);
        }

        // create widget item
        var widget = new JsonWidgetItem()
        {
            ProviderType = providerType,
            Name = _widgetResourceService.GetWidgetName(WidgetProviderType.DesktopWidgets3, widgetId, widgetType),
            Id = widgetId,
            Type = widgetType,
            Index = index,
            Pinned = true,
            Position = WidgetConstants.DefaultWidgetPosition,
            Size = _widgetResourceService.GetWidgetDefaultSize(widgetId, widgetType),
            DisplayMonitor = DisplayMonitor.GetPrimaryMonitorInfo(),
            Settings = _widgetResourceService.GetDefaultSettings(widgetId, widgetType),
        };

        // create widget window
        await CreateWidgetWindowAsync(widget);

        // update dashboard page
        if (updateDashboard)
        {
            RefreshDashboardPage(new DashboardViewModelNavigationParameter()
            {
                Event = DashboardViewModelNavigationParameter.UpdateEvent.Add,
                ProviderType = providerType,
                Id = widgetId,
                Type = widgetType,
                Index = index
            });
        }

        // save widget item
        await _appSettingsService.AddWidgetAsync(widget);
    }

    public async Task<int> AddWidgetAsync(WidgetViewModel widgetViewModel, Func<string, string, int, WidgetViewModel, Task>? action, bool updateDashboard)
    {
        var widgetList = _appSettingsService.GetWidgetsList();

        // get widget info
        var providerType = WidgetProviderType.Microsoft;
        var (_, widgetName, _, widgetId, widgetType) = widgetViewModel.GetWidgetProviderAndWidgetInfo();

        _log.Information("Add Microsoft widget (WidgetId: {WidgetId}, WidgetType: {WidgetType})", widgetId, widgetType);

        // find index tag
        var indexs = widgetList.Where(x => x.Equals(providerType, widgetId, widgetType)).Select(x => x.Index).ToList();
        indexs.Sort();
        var index = 0;
        foreach (var tag in indexs)
        {
            if (tag != index)
            {
                break;
            }
            index++;
        }

        // invoke action
        if (action != null)
        {
            await action(widgetId, widgetType, index, widgetViewModel);
        }

        // create widget item
        var item = new JsonWidgetItem()
        {
            ProviderType = providerType,
            Name = widgetName,
            Id = widgetId,
            Type = widgetType,
            Index = index,
            Pinned = true,
            Position = WidgetConstants.DefaultWidgetPosition,
            Size = _widgetResourceService.GetWidgetDefaultSize(widgetViewModel),
            DisplayMonitor = DisplayMonitor.GetPrimaryMonitorInfo(),
            Settings = new BaseWidgetSettings()
        };

        // create widget window
        await CreateWidgetWindowAsync(item, widgetViewModel);

        // update dashboard page
        if (updateDashboard)
        {
            RefreshDashboardPage(new DashboardViewModelNavigationParameter()
            {
                Event = DashboardViewModelNavigationParameter.UpdateEvent.Add,
                ProviderType = providerType,
                Id = widgetId,
                Type = widgetType,
                Index = index
            });
        }

        // save widget item
        await _appSettingsService.AddWidgetAsync(item);

        return index;
    }

    public async Task PinWidgetAsync(WidgetProviderType providerType, string widgetId, string widgetType, int widgetIndex, bool refresh)
    {
        _log.Information("Pin widget (ProviderType: {ProviderType}, WidgetId: {WidgetId}, WidgetType: {WidgetType}, WidgetIndex: {WidgetIndex})", providerType, widgetId, widgetType, widgetIndex);

        // get widget
        var widget = _appSettingsService.GetWidget(providerType, widgetId, widgetType, widgetIndex);

        // pin widget
        if (widget != null)
        {
            if (widget.ProviderType == WidgetProviderType.DesktopWidgets3)
            {
                // create widget window
                await CreateWidgetWindowAsync(widget);
            }
            else
            {
                // create widget view model and window
                await CreateWidgetViewModelAndWindowAsync(widget);
            }

            // update widget list
            await _appSettingsService.PinWidgetAsync(providerType, widgetId, widgetType, widgetIndex);

            // refresh dashboard page
            if (refresh)
            {
                RefreshDashboardPage(new DashboardViewModelNavigationParameter()
                {
                    Event = DashboardViewModelNavigationParameter.UpdateEvent.Pin,
                    ProviderType = providerType,
                    Id = widgetId,
                    Type = widgetType,
                    Index = widgetIndex
                });
            }
        }
        else
        {
            // add widget
            await AddWidgetAsync(widgetId, widgetType, null, false);

            // refresh dashboard page
            if (refresh)
            {
                RefreshDashboardPage(new DashboardViewModelNavigationParameter()
                {
                    Event = DashboardViewModelNavigationParameter.UpdateEvent.Add,
                    ProviderType = providerType,
                    Id = widgetId,
                    Type = widgetType,
                    Index = widgetIndex
                });
            }
        }
    }

    public async Task UnpinWidgetAsync(WidgetProviderType providerType, string widgetId, string widgetType, int widgetIndex, bool refresh)
    {
        _log.Information("Unpin widget (ProviderType: {ProviderType}, WidgetId: {WidgetId}, WidgetType: {WidgetType}, WidgetIndex: {WidgetIndex})", providerType, widgetId, widgetType, widgetIndex);

        // get widget runtime id
        var widgetRuntimeId = GetWidgetRuntimeId(providerType, widgetId, widgetType, widgetIndex);

        // close widget window
        await CloseWidgetWindowAsync(widgetRuntimeId, CloseEvent.Unpin);

        // refresh dashboard page
        if (refresh)
        {
            RefreshDashboardPage(new DashboardViewModelNavigationParameter()
            {
                Event = DashboardViewModelNavigationParameter.UpdateEvent.Unpin,
                ProviderType = providerType,
                Id = widgetId,
                Type = widgetType,
                Index = widgetIndex
            });
        }

        // update widget list
        await _appSettingsService.UnpinWidgetAsync(providerType, widgetId, widgetType, widgetIndex);
    }

    public async Task DeleteWidgetAsync(WidgetProviderType providerType, string widgetId, string widgetType, int widgetIndex, bool refresh)
    {
        _log.Information("Delete widget (ProviderType: {ProviderType}, WidgetId: {WidgetId}, WidgetType: {WidgetType}, WidgetIndex: {WidgetIndex})", providerType, widgetId, widgetType, widgetIndex);

        // delete microsoft widget if need
        var widgetViewModel = GetWidgetViewModel(widgetId, widgetType, widgetIndex);
        if (widgetViewModel != null)
        {
            // DevHome does this, but sometimes it can cause the thread await to hang, so we remove it.
            // Remove any custom state from the widget. In case the deletion fails, we won't show the widget anymore.
            // await widgetViewModel.Widget.SetCustomStateAsync(string.Empty);

            // Try delete widget
            await MicrosoftWidgetModel.TryDeleteWidgetAsync(widgetViewModel);
        }

        // get widget runtime id
        var widgetRuntimeId = GetWidgetRuntimeId(providerType, widgetId, widgetType, widgetIndex);

        // close widget window
        await CloseWidgetWindowAsync(widgetRuntimeId, CloseEvent.Delete);

        // refresh dashboard page
        if (refresh)
        {
            RefreshDashboardPage(new DashboardViewModelNavigationParameter()
            {
                Event = DashboardViewModelNavigationParameter.UpdateEvent.Delete,
                ProviderType = providerType,
                Id = widgetId,
                Type = widgetType,
                Index = widgetIndex
            });
        }

        // update widget list
        await _appSettingsService.DeleteWidgetAsync(providerType, widgetId, widgetType, widgetIndex);
    }

    public async Task RestartWidgetAsync(WidgetProviderType providerType, string widgetId, string widgetType, int widgetIndex)
    {
        _log.Information("Restart widget (ProviderType: {ProviderType}, WidgetId: {WidgetId}, WidgetType: {WidgetType}, WidgetIndex: {WidgetIndex})", providerType, widgetId, widgetType, widgetIndex);

        // get widget runtime id
        var widgetRuntimeId = GetWidgetRuntimeId(providerType, widgetId, widgetType, widgetIndex);

        // restart widget window
        if (!string.IsNullOrEmpty(widgetRuntimeId))
        {
            // close widget window
            await CloseWidgetWindowAsync(widgetRuntimeId, CloseEvent.Unpin);

            // get widget list
            var widgetList = _appSettingsService.GetWidgetsList();

            // find widget
            var widget = widgetList.FirstOrDefault(x => x.Equals(providerType, widgetId, widgetType, widgetIndex));
            if (widget != null)
            {
                if (widget.ProviderType == WidgetProviderType.DesktopWidgets3)
                {
                    // create widget window
                    await CreateWidgetWindowAsync(widget);
                }
                else if (widget.ProviderType == WidgetProviderType.Microsoft)
                {
                    // create widget view model and window
                    await CreateWidgetViewModelAndWindowAsync(widget);
                }
            }
        }
    }

    public WidgetWindow? GetWidgetWindow(string widgetRuntimeId)
    {
        // get widget window pair
        var widgetWindowPair = GetWidgetWindowPair(widgetRuntimeId);

        // get widget window
        if (widgetWindowPair != null)
        {
            return widgetWindowPair.Window;
        }

        return null;
    }

    #endregion

    #region Private

    private async Task CreateWidgetWindowAsync(JsonWidgetItem item)
    {
        // get widget info
        var providerType = item.ProviderType;
        var (widgetId, widgetType, widgetIndex) = (item.Id, item.Type, item.Index);

        _log.Debug("Creating Desktop Widgets 3 widget window (WidgetId: {WidgetId}, WidgetType: {WidgetType}, WidgetIndex: {WidgetIndex})", widgetId, widgetType, widgetIndex);

        // create widget info & register guid
        var widgetRuntimeId = StringUtils.GetRandomWidgetRuntimeId();
        var widgetContext = new WidgetContext(this)
        {
            ProviderType = providerType,
            Id = widgetRuntimeId,
            Type = widgetType,
        };
        var widgetInfo = new WidgetInfo(this)
        {
            WidgetContext = widgetContext
        };

        // change widget dictionary
        await AddWidgetToDictionaryAsync(() =>
        {
            // configure widget window lifecycle actions
            var lifecycleActions = new WindowsExtensions.WindowLifecycleActions()
            {
                Window_Creating = null,
                Window_Created = WidgetWindow_Created,
                Window_Closing = null,
                Window_Closed = null
            };

            // create widget window
            return WindowsExtensions.CreateWindow(() => new WidgetWindow(widgetRuntimeId, item), false, lifecycleActions);
        }, widgetRuntimeId, widgetInfo, providerType, widgetId, widgetType, widgetIndex);
    }

    private async Task CreateWidgetWindowAsync(JsonWidgetItem item, WidgetViewModel widgetViewModel)
    {
        // get widget info
        var providerType = item.ProviderType;
        var (widgetId, widgetType, widgetIndex) = (item.Id, item.Type, item.Index);

        _log.Debug("Creating Microsoft widget window (WidgetId: {WidgetId}, WidgetType: {WidgetType}, WidgetIndex: {WidgetIndex})", widgetId, widgetType, widgetIndex);

        // create widget info & register guid
        var widgetRuntimeId = widgetViewModel.Widget.Id;
        var widgetContext = new WidgetContext(this)
        {
            ProviderType = providerType,
            Id = widgetRuntimeId,
            Type = widgetType
        };
        var widgetInfo = new WidgetInfo(this)
        {
            WidgetContext = widgetContext
        };

        // change widget dictionary
        await AddWidgetToDictionaryAsync(() => 
        {
            // configure widget window lifecycle actions
            var lifecycleActions = new WindowsExtensions.WindowLifecycleActions()
            {
                Window_Creating = null,
                Window_Created = (window) => WidgetWindow_Created(window, widgetViewModel),
                Window_Closing = null,
                Window_Closed = null
            };

            // create widget window
            return WindowsExtensions.CreateWindow(() => new WidgetWindow(widgetRuntimeId, item, widgetViewModel), false, lifecycleActions);
        }, widgetRuntimeId, widgetInfo, providerType, widgetId, widgetType, widgetIndex);
    }

    private async Task AddWidgetToDictionaryAsync(Func<WidgetWindow> createWindow, string widgetRuntimeId, WidgetInfo widgetInfo, WidgetProviderType providerType, string widgetId, string widgetType, int widgetIndex)
    {
        // add widget dictionary
        PinnedWidgetWindowPairs.TryAdd(widgetRuntimeId, new WidgetWindowPair()
        {
            ProviderType = providerType,
            RuntimeId = widgetRuntimeId,
            WidgetId = widgetId,
            WidgetType = widgetType,
            WidgetIndex = widgetIndex,
            WidgetInfo = widgetInfo,
            Window = null!
        });

        // create window
        var window = createWindow();

        // add to widget window pair list
        var widgetWindowPair = GetWidgetWindowPair(providerType, widgetId, widgetType, widgetIndex);
        if (widgetWindowPair != null)
        {
            widgetWindowPair.Window = window;
        }

        // add to widget dictionary
        await _matchingWidgetNumberLock.WaitAsync();
        try
        {
            var key = new Tuple<WidgetProviderType, string, string>(providerType, widgetId, widgetType);
            if (MatchingWidgetNumber.TryGetValue(key, out var value))
            {
                MatchingWidgetNumber[key] = ++value;
            }
            else
            {
                MatchingWidgetNumber.Add(key, 1);
            }
        }
        finally
        {
            _matchingWidgetNumberLock.Release();
        }

        // invoke allow multiple widget changed event
        if (!_widgetResourceService.GetWidgetAllowMultiple(providerType, widgetId, widgetType))
        {
            AllowMultipleWidgetChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    #region Widget Window Lifecycle

    private async void WidgetWindow_Created(Window window)
    {
        if (window is WidgetWindow widgetWindow)
        {
            // activate window
            await _activationService.ActivateWindowAsync(widgetWindow);

            // get widget info
            var providerType = widgetWindow.ProviderType;
            var widgetId = widgetWindow.WidgetId;
            var widgetType = widgetWindow.WidgetType;
            var widgetIndex = widgetWindow.WidgetIndex;

            // set widget title
            widgetWindow.ViewModel.WidgetDisplayTitle = _widgetResourceService.GetWidgetName(providerType, widgetId, widgetType);

            // initialize window
            widgetWindow.Initialize();

            // set window style, size and position
            widgetWindow.IsResizable = false;
            WindowExtensions.Move(widgetWindow, -10000, -10000);

            // register load event handler
            widgetWindow.LoadCompleted += DesktopWidgets3WidgetWindow_LoadCompleted;

            // activate window
            widgetWindow.Activate();
        }
    }

    private void DesktopWidgets3WidgetWindow_LoadCompleted(object? sender, WidgetWindow.LoadCompletedEventArgs args)
    {
        if (sender is WidgetWindow widgetWindow)
        {
            // unregister load event handler
            widgetWindow.LoadCompleted -= DesktopWidgets3WidgetWindow_LoadCompleted;

            // get widget info
            var providerType = widgetWindow.ProviderType;
            var widgetId = widgetWindow.WidgetId;
            var widgetType = widgetWindow.WidgetType;
            var widgetIndex = widgetWindow.WidgetIndex;

            // parse event agrs
            var widgetRuntimeId = args.WidgetRuntimeId;
            var widgetPosition = args.WidgetActualPosition;
            var widgetSettings = args.WidgetSettings!;

            // set widget position
            widgetWindow.Position = widgetPosition;

            // set widget framework element
            var widgetContext = GetWidgetContext(providerType, widgetId, widgetType, widgetIndex)!;
            var frameworkElement = _widgetResourceService.CreateWidgetContent(widgetId, widgetContext);
            widgetWindow.ViewModel.WidgetFrameworkElement = frameworkElement;

            // invoke framework loaded event
            if (frameworkElement.IsLoaded)
            {
                WidgetFrameworkElement_Loaded(widgetId, widgetSettings, widgetContext, widgetWindow);
            }
            else
            {
                frameworkElement.Loaded += (s, e) => WidgetFrameworkElement_Loaded(widgetId, widgetSettings, widgetContext, widgetWindow);
            }
        }
    }

    private async void WidgetWindow_Created(Window window, WidgetViewModel widgetViewModel)
    {
        if (window is WidgetWindow widgetWindow)
        {
            // activate window
            await _activationService.ActivateWindowAsync(widgetWindow);

            // get widget info
            var providerType = widgetWindow.ProviderType;
            var widgetId = widgetWindow.WidgetId;
            var widgetType = widgetWindow.WidgetType;
            var widgetIndex = widgetWindow.WidgetIndex;

            // set widget title
            var (widgetName, _, _) = widgetViewModel.GetWidgetInfo();
            widgetWindow.ViewModel.WidgetDisplayTitle = widgetName;

            // initialize window
            widgetWindow.Initialize(widgetViewModel);

            // set window style, size and position
            widgetWindow.IsResizable = false;
            WindowExtensions.Move(widgetWindow, -10000, -10000);

            // register load event handler
            widgetWindow.LoadCompleted += MicrosoftWidgetWindow_LoadCompleted;

            // activate window
            widgetWindow.Activate();
        }
    }

    private void MicrosoftWidgetWindow_LoadCompleted(object? sender, WidgetWindow.LoadCompletedEventArgs args)
    {
        if (sender is WidgetWindow widgetWindow)
        {
            // unregister load event handler
            widgetWindow.LoadCompleted -= MicrosoftWidgetWindow_LoadCompleted;

            // get widget info
            var providerType = widgetWindow.ProviderType;
            var widgetId = widgetWindow.WidgetId;
            var widgetType = widgetWindow.WidgetType;
            var widgetIndex = widgetWindow.WidgetIndex;

            // parse event agrs
            var widgetRuntimeId = args.WidgetRuntimeId;
            var widgetPosition = args.WidgetActualPosition;
            var widgetViewModel = args.WidgetViewModel;

            // set widget position
            widgetWindow.Position = widgetPosition;

            // set widget framework element
            widgetWindow.ViewModel.InitializeWidgetViewmodel(widgetViewModel);

            // invoke framework loaded event
            if (widgetWindow.ViewModel.WidgetViewModel!.IsLoaded)
            {
                WidgetFrameworkElement_Loaded(widgetWindow);
            }
            else
            {
                widgetWindow.ViewModel.WidgetViewModel.Loaded += (s, e) => WidgetFrameworkElement_Loaded(widgetWindow);
            }
        }
    }

    private void WidgetFrameworkElement_Loaded(string widgetId, BaseWidgetSettings widgetSettings, WidgetContext widgetContext, WidgetWindow widgetWindow)
    {
        // invoke widget settings changed event
        _widgetResourceService.OnWidgetSettingsChanged(widgetId, new WidgetSettingsChangedArgs()
        {
            Settings = widgetSettings,
            WidgetContext = widgetContext!
        });

        // invoke widget activate & deactivate event
        widgetWindow.OnIsActiveChanged();
    }

    private static void WidgetFrameworkElement_Loaded(WidgetWindow widgetWindow)
    {
        // invoke widget activate & deactivate event
        widgetWindow.OnIsActiveChanged();
    }

    #endregion

    private async Task CreateWidgetViewModelAndWindowAsync(JsonWidgetItem item)
    {
        // get widget info
        var (widgetId, widgetType, widgetIndex) = (item.Id, item.Type, item.Index);

        _log.Debug("Creating Microsoft widget view model and window (WidgetId: {WidgetId}, WidgetType: {WidgetType}, WidgetIndex: {WidgetIndex})", widgetId, widgetType, widgetIndex);

        // create widget window
        var widgetViewModel = await _microsoftWidgetModel.GetWidgetViewModel(widgetId, widgetType, widgetIndex);
        if (widgetViewModel != null)
        {
            await CreateWidgetWindowAsync(item, widgetViewModel);
        }
        else
        {
            // If we cannot find the widget view model, we need to check if the installed providers has this widget
            var newWidgetDefinition = _microsoftWidgetModel.GetWidgetDefinition(widgetId, widgetType);
            if (newWidgetDefinition != null)
            {
                await _microsoftWidgetModel.AddWidgetsAsync(newWidgetDefinition, false, (wvm) =>
                {
                    return AddWidgetAsync(wvm, null, false);
                });
            }
            else
            {
                // If we cannot find the widget definition, we can do nothing because the provider is not installed.
            }
        }
    }

    private async Task CloseWidgetWindowAsync(string widgetRuntimeId, CloseEvent closeEvent)
    {
        // close widget window
        var widgetWindow = GetWidgetWindow(widgetRuntimeId);
        if (widgetWindow != null)
        {
            // get widget info
            var providerType = widgetWindow.ProviderType;
            var widgetId = widgetWindow.WidgetId;
            var widgetType = widgetWindow.WidgetType;
            var widgetIndex = widgetWindow.WidgetIndex;

            _log.Debug("Closing widget window (ProviderType: {ProviderType}, WidgetId: {WidgetId}, WidgetType: {WidgetType}, WidgetIndex: {WidgetIndex})", providerType, widgetId, widgetType, widgetIndex);

            // handle close event
            if (providerType == WidgetProviderType.DesktopWidgets3)
            {
                // register close event handler
                if (closeEvent == CloseEvent.Unpin)
                {
                    widgetWindow.Closed += (s, e) => _widgetResourceService.UnpinWidget(widgetId, widgetRuntimeId, GetWidgetSettings(widgetId, widgetType, widgetIndex)!);
                }
                else if (closeEvent == CloseEvent.Delete)
                {
                    widgetWindow.Closed += (s, e) => _widgetResourceService.DeleteWidget(widgetId, widgetRuntimeId, GetWidgetSettings(widgetId, widgetType, widgetIndex)!);
                }
            }
            else
            {
                // remove widget view model
                await _microsoftWidgetModel.RemoveWidgetViewModelAsync(widgetWindow.ViewModel.WidgetViewModel!);
            }

            // close window
            await WindowsExtensions.CloseWindowAsync(widgetWindow);

            // remove from widget window list
            PinnedWidgetWindowPairs.Remove(widgetRuntimeId, out var widgetWindowPair);

            // remove from widget dictionary
            await _matchingWidgetNumberLock.WaitAsync();
            try
            {
                var key = new Tuple<WidgetProviderType, string, string>(providerType, widgetId, widgetType);
                if (MatchingWidgetNumber.TryGetValue(key, out var value))
                {
                    MatchingWidgetNumber[key] = --value;
                }
            }
            finally
            {
                _matchingWidgetNumberLock.Release();
            }

            // invoke allow multiple widget changed event
            if (!_widgetResourceService.GetWidgetAllowMultiple(providerType, widgetId, widgetType))
            {
                AllowMultipleWidgetChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private enum CloseEvent
    {
        Unpin,
        Delete
    }

    #endregion

    #endregion

    #endregion

    #region Widget Setting

    public void NavigateToWidgetSettingPage(string widgetId, string widgetType, int widgetIndex)
    {
        // navigate to widget setting page
        _navigationService.NavigateTo(typeof(WidgetSettingPageViewModel).FullName!);

        // get widget setting pair
        var widgetSettingPair = TryGetWidgetSettingPair(widgetId, widgetType, widgetIndex);
        if (widgetSettingPair == null)
        {
            // create widget setting context & register guid
            var widgetSettingRuntimeId = StringUtils.GetRandomWidgetRuntimeId();
            var widgetSettingContext = new WidgetSettingContext()
            {
                Id = widgetSettingRuntimeId,
                Type = widgetType
            };

            // create widget setting framework
            var frameworkElement = _widgetResourceService.CreateWidgetSettingContent(widgetId, widgetSettingContext);

            // add to widget runtime id list & widget list
            widgetSettingPair = new WidgetSettingPair()
            {
                RuntimeId = widgetSettingRuntimeId,
                WidgetId = widgetId,
                WidgetType = widgetType,
                WidgetIndex = widgetIndex,
                WidgetSettingContext = widgetSettingContext,
                WidgetSettingContent = frameworkElement,
            };
            WidgetSettingPairs.TryAdd(widgetSettingRuntimeId, widgetSettingPair);
        }

        // set widget page & add to list & handle events
        var widgetSettingPage = _navigationService.Frame?.Content as WidgetSettingPage;
        if (widgetSettingPage != null)
        {
            // set widget setting framework element
            widgetSettingPage.ViewModel.WidgetFrameworkElement = widgetSettingPair.WidgetSettingContent;
            var widgetName = _widgetResourceService.GetWidgetName(WidgetProviderType.DesktopWidgets3, widgetId, widgetType);
            NavigationViewHeaderBehavior.SetHeaderLocalize(widgetSettingPage, false);
            NavigationViewHeaderBehavior.SetHeaderContext(widgetSettingPage, widgetName);

            // invoke widget settings changed event
            var widgetSettings = GetWidgetSettings(widgetId, widgetType, widgetIndex);
            _widgetResourceService.OnWidgetSettingsChanged(widgetId, new WidgetSettingsChangedArgs()
            {
                WidgetContext = widgetSettingPair.WidgetSettingContext,
                Settings = widgetSettings!,
            });
        }
    }

    private WidgetSettingPair? TryGetWidgetSettingPair(string widgetId, string widgetType, int widgetIndex)
    {
        // get widget setting pair
        var widgetSettingPair = GetWidgetSettingPair(widgetId, widgetType);

        // try get widget setting pair
        if (widgetSettingPair != null)
        {
            widgetSettingPair.WidgetIndex = widgetIndex;
            return widgetSettingPair;
        }

        return null;
    }

    #endregion

    #region Widget Settings

    public BaseWidgetSettings? GetWidgetSettings(string widgetId, string widgetType, int widgetIndex)
    {
        // get widget settings
        var widgetList = _appSettingsService.GetWidgetsList();
        var widget = widgetList.FirstOrDefault(x => x.Equals(WidgetProviderType.DesktopWidgets3, widgetId, widgetType, widgetIndex));

        return widget?.Settings.Clone();
    }

    public async Task UpdateWidgetSettingsAsync(string widgetId, string widgetType, int widgetIndex, BaseWidgetSettings settings)
    {
        // invoke widget settings changed event
        var widgetWindowPair = GetWidgetWindowPair(WidgetProviderType.DesktopWidgets3, widgetId, widgetType, widgetIndex);
        if (widgetWindowPair != null)
        {
            // if widget window exists
            _widgetResourceService.OnWidgetSettingsChanged(widgetId, new WidgetSettingsChangedArgs()
            {
                WidgetContext = widgetWindowPair.WidgetInfo.WidgetContext,
                Settings = settings,
            });
        }
        var widgetSettingPair = GetWidgetSettingPair(widgetId, widgetType);
        if (widgetSettingPair != null && widgetSettingPair.WidgetIndex == widgetIndex)
        {
            // if widget setting content exists and current index is the same
            _widgetResourceService.OnWidgetSettingsChanged(widgetId, new WidgetSettingsChangedArgs()
            {
                WidgetContext = widgetSettingPair.WidgetSettingContext,
                Settings = settings,
            });
        }

        // update widget list
        await _appSettingsService.UpdateWidgetSettingsAsync(WidgetProviderType.DesktopWidgets3, widgetId, widgetType, widgetIndex, settings);
    }

    #endregion

    #region Widget Edit Mode

    public async Task EnterEditModeAsync()
    {
        await _editModeLock.WaitAsync();

        _log.Information("Enter edit mode");

        // get pinned widget windows
        var pinnedWidgetWindows = GetPinnedWidgetWindows();

        // save original widget list
        _originalWidgetList.Clear();
        foreach (var widgetWindow in pinnedWidgetWindows)
        {
            // get widget info
            var providerType = widgetWindow.ProviderType;
            var widgetId = widgetWindow.WidgetId;
            var widgetType = widgetWindow.WidgetType;
            var widgetIndex = widgetWindow.WidgetIndex;

            // save original widget
            var displayMonitor = DisplayMonitor.GetMonitorInfo(widgetWindow);
            _originalWidgetList.Add(new JsonWidgetItem()
            {
                ProviderType = providerType,
                Name = _widgetResourceService.GetWidgetName(providerType, widgetId, widgetType),
                Id = widgetId,
                Type = widgetType,
                Index = widgetIndex,
                Pinned = true,
                Position = WidgetWindow.GetWidgetJsonPosition(widgetWindow.Position, displayMonitor),
                Size = widgetWindow.ContentSize,
                DisplayMonitor = displayMonitor,
                Settings = null!
            });
        }

        // set edit mode for all widgets
        await pinnedWidgetWindows.EnqueueOrInvokeAsync((window) =>
        {
            window.SetEditMode(true);
        }, Microsoft.UI.Dispatching.DispatcherQueuePriority.High);

        // hide main window & show edit mode overlay window
        await App.MainWindow.EnqueueOrInvokeAsync(async (mainWindow) =>
        {
            // hide main window
            if (mainWindow.Visible)
            {
                await WindowsExtensions.CloseWindowAsync(mainWindow);
                _restoreMainWindow = true;
            }

            // show edit mode overlay window
            App.EditModeWindow.Show();
        }, Microsoft.UI.Dispatching.DispatcherQueuePriority.High);

        // set flag
        _inEditMode = true;

        _editModeLock.Release();
    }

    public async Task SaveAndExitEditMode()
    {
        await _editModeLock.WaitAsync();

        _log.Information("Save and exit edit mode");

        // get pinned widget windows
        var pinnedWidgetWindows = GetPinnedWidgetWindows();

        // restore edit mode for all widgets
        await pinnedWidgetWindows.EnqueueOrInvokeAsync((window) =>
        {
            window.SetEditMode(false);
        }, Microsoft.UI.Dispatching.DispatcherQueuePriority.High);

        // hide edit mode overlay window
        App.EditModeWindow?.Hide();

        // restore main window if needed
        if (_restoreMainWindow)
        {
            App.MainWindow.Show();
            _restoreMainWindow = false;
        }

        // save widget list
        List<JsonWidgetItem> widgetList = [];
        foreach (var widgetWindow in pinnedWidgetWindows)
        {
            // get widget info
            var providerType = widgetWindow.ProviderType;
            var widgetId = widgetWindow.WidgetId;
            var widgetType = widgetWindow.WidgetType;
            var widgetIndex = widgetWindow.WidgetIndex;

            // find original widget
            var displayMonitor = DisplayMonitor.GetMonitorInfo(widgetWindow);
            widgetList.Add(new JsonWidgetItem()
            {
                ProviderType = providerType,
                Name = _widgetResourceService.GetWidgetName(providerType, widgetId, widgetType),
                Id = widgetId,
                Type = widgetType,
                Index = widgetIndex,
                Pinned = true,
                Position = WidgetWindow.GetWidgetJsonPosition(widgetWindow.Position, displayMonitor),
                Size = widgetWindow.ContentSize,
                DisplayMonitor = displayMonitor,
                Settings = null!
            });
        }
        await _appSettingsService.UpdateWidgetsListIgnoreSettingsAsync(widgetList);

        _inEditMode = false;

        _editModeLock.Release();
    }

    public async Task CancelChangesAndExitEditModeAsync()
    {
        await _editModeLock.WaitAsync();

        _log.Information("Cancel changes and exit edit mode");

        // get pinned widget windows
        var pinnedWidgetWindows = GetPinnedWidgetWindows();

        // restore position, size, edit mode for all widgets
        await pinnedWidgetWindows.EnqueueOrInvokeAsync((window) =>
        {
            // set edit mode for all widgets
            window.SetEditMode(false);

            // get widget info
            var providerType = window.ProviderType;
            var widgetId = window.WidgetId;
            var widgetType = window.WidgetType;
            var widgetIndex = window.WidgetIndex;

            // find original widget
            var originalWidget = _originalWidgetList.FirstOrDefault(x => x.Equals(providerType, widgetId, widgetType, widgetIndex));

            // restore position and size
            if (originalWidget != null)
            {
                var positionX = WidgetWindow.GetWidgetActualPositionX(originalWidget.Position.X, originalWidget.DisplayMonitor);
                var positionY = WidgetWindow.GetWidgetActualPositionY(originalWidget.Position.Y, originalWidget.DisplayMonitor);
                window.Position = new PointInt32(positionX, positionY);
                window.ContentSize = originalWidget.Size;
                window.Show();
            };
        }, Microsoft.UI.Dispatching.DispatcherQueuePriority.High);

        // hide edit mode overlay window
        App.EditModeWindow?.Hide();

        // restore main window if needed
        if (_restoreMainWindow)
        {
            App.MainWindow.Show();
            _restoreMainWindow = false;
        }

        _inEditMode = false;

        _editModeLock.Release();
    }

    public async Task CheckEditModeAsync()
    {
        await _editModeLock.WaitAsync();

        _log.Information("Check edit mode");

        if (_inEditMode)
        {
            App.MainWindow.Show();
            App.MainWindow.BringToFront();
            if (await DialogFactory.ShowQuitEditModeDialogAsync() == WidgetDialogResult.Left)
            {
                await SaveAndExitEditMode();
            }
        }

        _editModeLock.Release();
    }

    #endregion

    #region Dashboard

    private void RefreshDashboardPage(object parameter)
    {
        var dashboardPageKey = typeof(DashboardPageViewModel).FullName!;
        App.MainWindow.DispatcherQueue.TryEnqueue(() =>
        {
            var currentKey = _navigationService.GetCurrentPageKey();
            if (currentKey == dashboardPageKey)
            {
                _navigationService.NavigateTo(dashboardPageKey, parameter);
            }
            else
            {
                _navigationService.SetNextParameter(dashboardPageKey, parameter);
            }
        });
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
                _matchingWidgetNumberLock.Dispose();
                _editModeLock.Dispose();
                _initWidgetsCancellationTokenSource.Dispose();

                PinnedWidgetWindowPairs.Clear();
                WidgetSettingPairs.Clear();
                _originalWidgetList.Clear();
            }

            _disposed = true;
        }
    }

    #endregion
}
