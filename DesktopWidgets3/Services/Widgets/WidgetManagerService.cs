using System.Collections.Concurrent;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Serilog;

namespace DesktopWidgets3.Services.Widgets;

internal class WidgetManagerService(MicrosoftWidgetModel microsoftWidgetModel, IActivationService activationService, IAppSettingsService appSettingsService, INavigationService navigationService, IWidgetResourceService widgetResourceService) : IWidgetManagerService
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(WidgetManagerService));

    private readonly MicrosoftWidgetModel _microsoftWidgetModel = microsoftWidgetModel;

    private readonly IActivationService _activationService = activationService;
    private readonly IAppSettingsService _appSettingsService = appSettingsService;
    private readonly INavigationService _navigationService = navigationService;
    private readonly IWidgetResourceService _widgetResourceService = widgetResourceService;

    private readonly ConcurrentDictionary<string, WidgetWindowPair> PinnedWidgetWindowPairs = [];
    private readonly ConcurrentDictionary<string, WidgetSettingPair> WidgetSettingPairs = [];

    private readonly List<JsonWidgetItem> _originalWidgetList = [];
    private bool _inEditMode = false;
    private bool _restoreMainWindow = false;

    #region Initialization

    public async void InitializePinnedWidgets()
    {
        // get microsoft widgets 3 widget list & microsoft widget list
        var widgetList = _appSettingsService.GetWidgetsList();
        var desktopWidgets3WidgetList = widgetList.Where(x => x.ProviderType == WidgetProviderType.DesktopWidgets3).ToList();
        var microsoftWidgetList = widgetList.Where(x => x.ProviderType == WidgetProviderType.Microsoft).ToList();

        // initialize desktop widgets 3 widgets
        foreach (var widget in desktopWidgets3WidgetList)
        {
            if (widget.ProviderType == WidgetProviderType.DesktopWidgets3 && widget.Pinned)
            {
                CreateWidgetWindow(widget);
            }
        }

        // initialize microsoft widgets
        await _microsoftWidgetModel.InitializePinnedWidgetsAsync((widget, index) => CreateWidgetWindowAsync(microsoftWidgetList, index, widget));
        // We don't delete microsoft widget json items that aren't in the system widget storage,
        // because we need to keep the settings of the deleted widgets to restore them when the user re-adds them.
    }

    private async Task CreateWidgetWindowAsync(List<JsonWidgetItem> itemList, int index, WidgetViewModel widgetViewModel)
    {
        // get widget info
        var providerType = WidgetProviderType.Microsoft;
        var (_, _, _, widgetId, widgetType) = widgetViewModel.GetWidgetProviderAndWidgetInfo();
        var widgetIndex = index;

        // find item
        var item = itemList.FirstOrDefault(x => x.Equals(providerType, widgetId, widgetType, widgetIndex));
        if (item != null)
        {
            if (item.Pinned)
            {
                // create widget window
                CreateWidgetWindow(item, widgetViewModel);
            }
            else
            {
                // TODO: Add support for microsoft widgets unpinned.
            }
        }
        else
        {
            // add widget
            await AddWidgetAsync(widgetViewModel, null, false);
        }
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

    #region Is Navigated

    public bool GetWidgetSettingIsNavigated(string widgetId, string widgetType)
    {
        return true;
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

    #region All Widgets Management

    public async Task RestartWidgetsAsync()
    {
        // close all widgets
        await CloseAllWidgetWindowsAsync();

        // enable all enabled widgets
        InitializePinnedWidgets();
    }

    public async Task CloseAllWidgetsAsync()
    {
        // close all widgets
        await CloseAllWidgetWindowsAsync();

        // clear all lists
        PinnedWidgetWindowPairs.Clear();
        WidgetSettingPairs.Clear();
        _originalWidgetList.Clear();
        
        // dispose microsoft widget model
        _microsoftWidgetModel.Dispose();
    }

    private async Task CloseAllWidgetWindowsAsync()
    {
        // close all windows
        await GetPinnedWidgetWindows().EnqueueOrInvokeAsync(async (window) =>
        {
            // TODO: Check if will this set open widgets to unpin?
            await CloseWidgetWindowAsync(window.RuntimeId, CloseEvent.Unpin);
        }, Microsoft.UI.Dispatching.DispatcherQueuePriority.High);

        // unload microsoft widget model
        await _microsoftWidgetModel.OnUnloadedAsync();
    }

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

    #region Single Widget Management

    #region Public

    public async Task AddWidgetAsync(string widgetId, string widgetType, Func<string, string, int, Task>? action, bool updateDashboard)
    {
        var widgetList = _appSettingsService.GetWidgetsList();

        // get widget info
        var providerType = WidgetProviderType.DesktopWidgets3;

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
        CreateWidgetWindow(widget);

        // update dashboard page
        if (updateDashboard)
        {
            RefreshDashboardPage(new DashboardViewModelNavigationParameter()
            {
                Event = DashboardViewModelNavigationParameter.UpdateEvent.Pin,
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
            Size = RectSize.NULL,
            DisplayMonitor = DisplayMonitor.GetPrimaryMonitorInfo(),
            Settings = new BaseWidgetSettings()
        };

        // create widget window
        CreateWidgetWindow(item, widgetViewModel);

        // update dashboard page
        if (updateDashboard)
        {
            RefreshDashboardPage(new DashboardViewModelNavigationParameter()
            {
                Event = DashboardViewModelNavigationParameter.UpdateEvent.Pin,
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
        // TODO: Add support for micrsoft widgets pin.
        if (providerType == WidgetProviderType.Microsoft)
        {
            return;
        }

        // get widget
        var widget = _appSettingsService.GetWidget(providerType, widgetId, widgetType, widgetIndex);

        // pin widget
        if (widget != null)
        {
            // create widget window
            CreateWidgetWindow(widget);

            // update widget list
            await _appSettingsService.PinWidgetAsync(providerType, widgetId, widgetType, widgetIndex);
        }
        else
        {
            // add widget
            await AddWidgetAsync(widgetId, widgetType, null, false);
        }

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

    public async Task UnpinWidgetAsync(WidgetProviderType providerType, string widgetId, string widgetType, int widgetIndex, bool refresh)
    {
        // TODO: Add support for micrsoft widgets unpin.
        if (providerType == WidgetProviderType.Microsoft)
        {
            return;
        }

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
        // delete microsoft widget if need
        var widgetViewModel = GetWidgetViewModel(widgetId, widgetType, widgetIndex);
        if (widgetViewModel != null)
        {
            // Remove any custom state from the widget. In case the deletion fails, we won't show the widget anymore.
            await widgetViewModel.Widget.SetCustomStateAsync(string.Empty);

            // Remove the widget from the list before deleting, otherwise the widget will
            // have changed and the collection won't be able to find it to remove it.
            widgetViewModel.Dispose();
            _microsoftWidgetModel.PinnedWidgets.Remove(widgetViewModel);

            // Try delete widget
            await _microsoftWidgetModel.TryDeleteWidgetAsync(widgetViewModel);
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

    private void CreateWidgetWindow(JsonWidgetItem item)
    {
        // get widget info
        var (widgetId, widgetType, widgetIndex) = (item.Id, item.Type, item.Index);

        // create widget info & register guid
        var widgetRuntimeId = StringUtils.GetRandomWidgetRuntimeId();
        var widgetContext = new WidgetContext(this)
        {
            ProviderType = WidgetProviderType.DesktopWidgets3,
            Id = widgetRuntimeId,
            Type = widgetType,
        };
        var widgetInfo = new WidgetInfo(this)
        {
            WidgetContext = widgetContext
        };

        // add to widget window list
        PinnedWidgetWindowPairs.TryAdd(widgetRuntimeId, new WidgetWindowPair()
        {
            ProviderType = WidgetProviderType.DesktopWidgets3,
            RuntimeId = widgetRuntimeId,
            WidgetId = widgetId,
            WidgetType = widgetType,
            WidgetIndex = widgetIndex,
            WidgetInfo = widgetInfo,
            Window = null!,
            MenuFlyout = null!
        });

        // configure widget window lifecycle actions
        var lifecycleActions = new WindowsExtensions.WindowLifecycleActions()
        {
            Window_Creating = null,
            Window_Created = WidgetWindow_Created,
            Window_Closing = null,
            Window_Closed = null
        };

        // create widget window
        WindowsExtensions.CreateWindow(() => new WidgetWindow(widgetRuntimeId, item), _appSettingsService.MultiThread, lifecycleActions);
    }

    private void CreateWidgetWindow(JsonWidgetItem item, WidgetViewModel widgetViewModel)
    {
        // get widget info
        var (widgetId, widgetType, widgetIndex) = (item.Id, item.Type, item.Index);

        // create widget info & register guid
        var widgetRuntimeId = widgetViewModel.Widget.Id;
        var widgetContext = new WidgetContext(this)
        {
            ProviderType = WidgetProviderType.Microsoft,
            Id = widgetRuntimeId,
            Type = widgetType
        };
        var widgetInfo = new WidgetInfo(this)
        {
            WidgetContext = widgetContext
        };

        // add to widget window list
        PinnedWidgetWindowPairs.TryAdd(widgetRuntimeId, new WidgetWindowPair()
        {
            ProviderType = WidgetProviderType.Microsoft,
            RuntimeId = widgetRuntimeId,
            WidgetId = widgetId,
            WidgetType = widgetType,
            WidgetIndex = widgetIndex,
            WidgetInfo = widgetInfo,
            Window = null!,
            MenuFlyout = null!
        });

        // configure widget window lifecycle actions
        var lifecycleActions = new WindowsExtensions.WindowLifecycleActions()
        {
            Window_Creating = null,
            Window_Created = (window) => WidgetWindow_Created(window, widgetViewModel),
            Window_Closing = null,
            Window_Closed = null
        };

        // create widget window
        WindowsExtensions.CreateWindow(() => new WidgetWindow(widgetRuntimeId, item, widgetViewModel), _appSettingsService.MultiThread, lifecycleActions);
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
            var menuFlyout = await GetWidgetMenuFlyoutAsync(widgetWindow);
            widgetWindow.Initialize(menuFlyout);

            // set window style, size and position
            widgetWindow.IsResizable = false;
            WindowExtensions.Move(widgetWindow, -10000, -10000);

            // register load event handler
            widgetWindow.LoadCompleted += DesktopWidgets3WidgetWindow_LoadCompleted;

            // activate window
            widgetWindow.Activate();

            // add to widget window pair list
            var widgetWindowPair = GetWidgetWindowPair(providerType, widgetId, widgetType, widgetIndex);
            if (widgetWindowPair != null)
            {
                widgetWindowPair.Window = widgetWindow;
                widgetWindowPair.MenuFlyout = menuFlyout;
            }
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
            var widgetPosition = args.WidgetPosition;
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
            var menuFlyout = await GetWidgetMenuFlyoutAsync(widgetWindow);
            widgetWindow.Initialize(menuFlyout);

            // set window style, size and position
            widgetWindow.IsResizable = false;
            WindowExtensions.Move(widgetWindow, -10000, -10000);

            // register load event handler
            widgetWindow.LoadCompleted += MicrosoftWidgetWindow_LoadCompleted;

            // activate window
            widgetWindow.Activate();

            // add to widget window pair list
            var widgetWindowPair = GetWidgetWindowPair(providerType, widgetId, widgetType, widgetIndex);
            if (widgetWindowPair != null)
            {
                widgetWindowPair.Window = widgetWindow;
                widgetWindowPair.MenuFlyout = menuFlyout;
            }

            // add to microsoft widget model
            _microsoftWidgetModel.PinnedWidgets.Add(widgetViewModel);
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
            var widgetPosition = args.WidgetPosition;
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

            // register close event handler
            if (closeEvent == CloseEvent.Unpin)
            {
                widgetWindow.Closed += (s, e) => _widgetResourceService.UnpinWidget(widgetId, widgetRuntimeId, GetWidgetSettings(widgetId, widgetType, widgetIndex)!);
            }
            else if (closeEvent == CloseEvent.Delete)
            {
                widgetWindow.Closed += (s, e) => _widgetResourceService.DeleteWidget(widgetId, widgetRuntimeId, GetWidgetSettings(widgetId, widgetType, widgetIndex)!);
            }

            // close window
            await WindowsExtensions.CloseWindowAsync(widgetWindow);

            // remove from widget window pair list
            PinnedWidgetWindowPairs.Remove(widgetRuntimeId, out var widgetWindowPair);
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
            var widgetSettingContext = new WidgetSettingContext(this)
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

    #region Widget Menu

    private async Task<MenuFlyout> GetWidgetMenuFlyoutAsync(WidgetWindow widgetWindow)
    {
        // get widget info
        var providerType = widgetWindow.ProviderType;
        var widgetId = widgetWindow.WidgetId;
        var widgetType = widgetWindow.WidgetType;

        // create menu flyout
        var menuFlyout = new MenuFlyout
        {
            Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft
        };

        // add menu items
        // pin & delete & customize
        AddUnpinDeleteItemsToWidgetMenu(menuFlyout, widgetWindow);
        if (_widgetResourceService.GetWidgetIsCustomizable(providerType, widgetId, widgetType))
        {
            AddCustomizeToWidgetMenu(menuFlyout, widgetWindow);
        }

        // size
        if (providerType == WidgetProviderType.Microsoft)
        {
            // TODO: Add support for widget size menu items.
            /*menuFlyout.Items.Add(new MenuFlyoutSeparator());
            await AddSizesToWidgetMenuAsync(menuFlyout, widgetWindow);*/
        }

        // layout
        menuFlyout.Items.Add(new MenuFlyoutSeparator());
        AddLayoutItemsToWidgetMenu(menuFlyout, widgetWindow);

        // restart
#if DEBUG
        menuFlyout.Items.Add(new MenuFlyoutSeparator());
        AddRestartItemsToWidgetMenu(menuFlyout, widgetWindow);
#endif

        return menuFlyout;
    }

    #region Unpin & Delete

    private void AddUnpinDeleteItemsToWidgetMenu(MenuFlyout menuFlyout, WidgetWindow widgetWindow)
    {
        var unpinIcon = new FontIcon()
        {
            Glyph = "\uE77A"
        };
        var unpinWidgetMenuItem = new MenuFlyoutItem
        {
            Tag = widgetWindow,
            Text = "MenuFlyoutItem_UnpinWidget.Text".GetLocalizedString(),
            Icon = unpinIcon
        };
        unpinWidgetMenuItem.Click += OnUnpinWidgetClick;
        menuFlyout.Items.Add(unpinWidgetMenuItem);

        var deleteIcon = new FontIcon()
        {
            Glyph = "\uE74D"
        };
        var deleteWidgetMenuItem = new MenuFlyoutItem
        {
            Tag = widgetWindow,
            Text = "MenuFlyoutItem_DeleteWidget.Text".GetLocalizedString(),
            Icon = deleteIcon
        };
        deleteWidgetMenuItem.Click += OnDeleteWidgetClick;
        menuFlyout.Items.Add(deleteWidgetMenuItem);
    }

    private async void OnUnpinWidgetClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem deleteMenuItem && deleteMenuItem?.Tag is WidgetWindow widgetWindow)
        {
            // get widget info
            var providerType = widgetWindow.ProviderType;
            var widgetId = widgetWindow.WidgetId;
            var widgetType = widgetWindow.WidgetType;
            var widgetIndex = widgetWindow.WidgetIndex;

            // unpin widget
            await UnpinWidgetAsync(providerType, widgetId, widgetType, widgetIndex, true);
        }
    }

    private async void OnDeleteWidgetClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem deleteMenuItem && deleteMenuItem?.Tag is WidgetWindow widgetWindow)
        {
            // get widget info
            var providerType = widgetWindow.ProviderType;
            var widgetId = widgetWindow.WidgetId;
            var widgetType = widgetWindow.WidgetType;
            var widgetIndex = widgetWindow.WidgetIndex;

            // delete widget
            await DialogFactory.ShowDeleteWidgetFullScreenDialogAsync(async () =>
            {
                await DeleteWidgetAsync(providerType, widgetId, widgetType, widgetIndex, true);
            });
        }
    }

    #endregion

    #region Customize

    private void AddCustomizeToWidgetMenu(MenuFlyout widgetMenuFlyout, WidgetWindow widgetWindow)
    {
        var icon = new FontIcon()
        {
            Glyph = "\xE70F"
        };
        var customizeWidgetItem = new MenuFlyoutItem
        {
            Tag = widgetWindow,
            Icon = icon,
            Text = "MenuFlyoutItem_CustomizeWidget.Text".GetLocalizedString()
        };
        customizeWidgetItem.Click += OnCustomizeWidgetClick;
        widgetMenuFlyout.Items.Add(customizeWidgetItem);
    }

    private async void OnCustomizeWidgetClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem customizeMenuItem && customizeMenuItem?.Tag is WidgetWindow widgetWindow)
        {
            var providerType = widgetWindow.ProviderType;
            if (providerType == WidgetProviderType.DesktopWidgets3)
            {
                // TODO: Add support for desktop widgets customization and combine this codes in one function.
            }
            else
            {
                await widgetWindow.ViewModel.WidgetViewModel!.Widget.NotifyCustomizationRequestedAsync();
            }
        }
    }

    #endregion

    #region Size

    // TODO: Add support for widget size menu items.
    /*private async Task AddSizesToWidgetMenuAsync(MenuFlyout widgetMenuFlyout, WidgetWindow widgetWindow)
    {
        var widgetViewModel = widgetWindow.ViewModel.WidgetViewModel;
        if (widgetViewModel is null)
        {
            // If we can't get the widgetViewModel, bail and don't show sizes.
            return;
        }

        var widgetHostingService = DependencyExtensions.GetRequiredService<IWidgetHostingService>();
        var unsafeWidgetDefinition = await widgetHostingService.GetWidgetDefinitionAsync(widgetViewModel.Widget.DefinitionId);
        if (unsafeWidgetDefinition == null)
        {
            // If we can't get the widgetDefinition, bail and don't show sizes.
            return;
        }

        var widgetDefinitionId = await ComSafeWidgetDefinition.GetIdFromUnsafeWidgetDefinitionAsync(unsafeWidgetDefinition);
        if (string.IsNullOrEmpty(widgetDefinitionId))
        {
            // If we can't get the widgetDefinitionId, bail and don't show sizes.
            return;
        }

        var comSafeWidgetDefinition = new ComSafeWidgetDefinition(widgetDefinitionId);
        if (!await comSafeWidgetDefinition.PopulateAsync())
        {
            // If we can't populate the widgetDefinition, bail and don't show sizes.
            return;
        }

        var capabilities = await comSafeWidgetDefinition.GetWidgetCapabilitiesAsync();
        var sizeMenuItems = new List<SelectableMenuFlyoutItem>();

        // Add the three possible sizes. Each side should only be enabled if it is included in the widget's capabilities.
        if (capabilities.Any(cap => cap.Size == WidgetSize.Small))
        {
            var menuItemSmall = new SelectableMenuFlyoutItem
            {
                Tag = WidgetSize.Small,
                Text = "SmallWidgetMenuText".GetLocalizedString(Constants.DevHomeDashboard),
            };
            menuItemSmall.Click += OnMenuItemSizeClick;
            menuItemSmall.SetValue(AutomationProperties.AutomationIdProperty, "SmallWidgetButton");
            widgetMenuFlyout.Items.Add(menuItemSmall);
            sizeMenuItems.Add(menuItemSmall);
        }

        if (capabilities.Any(cap => cap.Size == WidgetSize.Medium))
        {
            var menuItemMedium = new SelectableMenuFlyoutItem
            {
                Tag = WidgetSize.Medium,
                Text = "MediumWidgetMenuText".GetLocalizedString(Constants.DevHomeDashboard),
            };
            menuItemMedium.Click += OnMenuItemSizeClick;
            menuItemMedium.SetValue(AutomationProperties.AutomationIdProperty, "MediumWidgetButton");
            widgetMenuFlyout.Items.Add(menuItemMedium);
            sizeMenuItems.Add(menuItemMedium);
        }

        if (capabilities.Any(cap => cap.Size == WidgetSize.Large))
        {
            var menuItemLarge = new SelectableMenuFlyoutItem
            {
                Tag = WidgetSize.Large,
                Text = "LargeWidgetMenuText".GetLocalizedString(Constants.DevHomeDashboard),
            };
            menuItemLarge.Click += OnMenuItemSizeClick;
            menuItemLarge.SetValue(AutomationProperties.AutomationIdProperty, "LargeWidgetButton");
            widgetMenuFlyout.Items.Add(menuItemLarge);
            sizeMenuItems.Add(menuItemLarge);
        }

        // Mark current widget size.
        _currentSelectedSize = sizeMenuItems.FirstOrDefault(x => (WidgetSize)x.Tag == widgetViewModel.WidgetSize);
        if (_currentSelectedSize is not null)
        {
            MarkSize(_currentSelectedSize);
        }
    }

    private async void OnMenuItemSizeClick(object sender, RoutedEventArgs e)
    {
        if (sender is SelectableMenuFlyoutItem menuSizeItem)
        {
            if (menuSizeItem.DataContext is WidgetViewModel widgetViewModel)
            {
                // Unset mark on current size.
                if (_currentSelectedSize is not null)
                {
                    _currentSelectedSize.Icon = null;
                    var peer = FrameworkElementAutomationPeer.FromElement(_currentSelectedSize) as SelectableMenuFlyoutItemAutomationPeer;
                    peer?.RemoveFromSelection();
                }

                // Resize widget.
                var size = (WidgetSize)menuSizeItem.Tag;
                widgetViewModel.WidgetSize = size;
                await widgetViewModel.Widget.SetSizeAsync(size);
                SetScaledWidthAndHeight(_uiSettings.TextScaleFactor);

                // Set mark on new size.
                _currentSelectedSize = menuSizeItem;
                MarkSize(_currentSelectedSize);
            }
        }
    }

    private static void MarkSize(SelectableMenuFlyoutItem menuSizeItem)
    {
        var fontIcon = new FontIcon
        {
            Glyph = "\xE915",
        };
        menuSizeItem.Icon = fontIcon;
        var peer = FrameworkElementAutomationPeer.FromElement(menuSizeItem) as SelectableMenuFlyoutItemAutomationPeer;
        peer?.AddToSelection();
    }*/

    #endregion

    #region Layout

    private void AddLayoutItemsToWidgetMenu(MenuFlyout menuFlyout, WidgetWindow widgetWindow)
    {
        var layoutIcon = new FontIcon()
        {
            Glyph = "\uF0E2"
        };
        var editLayoutMenuItem = new MenuFlyoutItem
        {
            Tag = widgetWindow,
            Icon = layoutIcon,
            Text = "MenuFlyoutItem_EditWidgetsLayout.Text".GetLocalizedString()
        };
        editLayoutMenuItem.Click += OnEditWidgetsLayoutClick;
        menuFlyout.Items.Add(editLayoutMenuItem);
    }

    private void OnEditWidgetsLayoutClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem deleteMenuItem && deleteMenuItem?.Tag is WidgetWindow)
        {
            EnterEditMode();
        }  
    }

    #endregion

    #region Restart

    private void AddRestartItemsToWidgetMenu(MenuFlyout menuFlyout, WidgetWindow widgetWindow)
    {
        var restartIcon = new FontIcon()
        {
            Glyph = "\uE72C"
        };
        var restartWidgetMenuItem = new MenuFlyoutItem
        {
            Tag = widgetWindow,
            Text = "MenuFlyoutItem_RestartWidget.Text".GetLocalizedString(),
            Icon = restartIcon,
        };
        restartWidgetMenuItem.Click += OnRestartWidgetClick;
        menuFlyout.Items.Add(restartWidgetMenuItem);

        restartIcon = new FontIcon()
        {
            Glyph = "\uE72C"
        };
        var restartWidgetsMenuItem = new MenuFlyoutItem
        {
            Tag = widgetWindow,
            Text = "MenuFlyoutItem_RestartWidgets.Text".GetLocalizedString(),
            Icon = restartIcon
        };
        restartWidgetsMenuItem.Click += OnRestartWidgetsClick;
        menuFlyout.Items.Add(restartWidgetsMenuItem);
    }

    private async void OnRestartWidgetClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem deleteMenuItem && deleteMenuItem?.Tag is WidgetWindow widgetWindow)
        {
            // close widget window
            var widgetRuntimeId = widgetWindow.RuntimeId;
            await CloseWidgetWindowAsync(widgetRuntimeId, CloseEvent.Unpin);

            // get widget list
            var widgetList = _appSettingsService.GetWidgetsList();

            // get widget info
            var providerType = widgetWindow.ProviderType;
            var widgetId = widgetWindow.WidgetId;
            var widgetType = widgetWindow.WidgetType;
            var widgetIndex = widgetWindow.WidgetIndex;

            // find widget
            var widget = widgetList.FirstOrDefault(x => x.Equals(providerType, widgetId, widgetType, widgetIndex));
            if (widget != null)
            {
                CreateWidgetWindow(widget);
            }
        }
    }

    private async void OnRestartWidgetsClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem deleteMenuItem && deleteMenuItem?.Tag is WidgetWindow)
        {
            await RestartWidgetsAsync();
        }
    }

    #endregion

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

    public async void EnterEditMode()
    {
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
            _originalWidgetList.Add(new JsonWidgetItem()
            {
                ProviderType = providerType,
                Name = _widgetResourceService.GetWidgetName(providerType, widgetId, widgetType),
                Id = widgetId,
                Type = widgetType,
                Index = widgetIndex,
                Pinned = true,
                Position = widgetWindow.Position,
                Size = widgetWindow.ContentSize,
                DisplayMonitor = DisplayMonitor.GetMonitorInfo(widgetWindow),
                Settings = null!,
            });
        }

        // set flag
        _inEditMode = true;

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
    }

    public async Task SaveAndExitEditMode()
    {
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
            widgetList.Add(new JsonWidgetItem()
            {
                ProviderType = providerType,
                Name = _widgetResourceService.GetWidgetName(providerType, widgetId, widgetType),
                Id = widgetId,
                Type = widgetType,
                Index = widgetIndex,
                Pinned = true,
                Position = widgetWindow.Position,
                Size = widgetWindow.ContentSize,
                DisplayMonitor = DisplayMonitor.GetMonitorInfo(widgetWindow),
                Settings = null!,
            });
        }
        await _appSettingsService.UpdateWidgetsListIgnoreSettingsAsync(widgetList);

        _inEditMode = false;
    }

    public async void CancelChangesAndExitEditMode()
    {
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
                window.Position = originalWidget.Position;
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
    }

    public async Task CheckEditModeAsync()
    {
        if (_inEditMode)
        {
            App.MainWindow.Show();
            App.MainWindow.BringToFront();
            if (await DialogFactory.ShowQuitEditModeDialogAsync() == WidgetDialogResult.Left)
            {
                await SaveAndExitEditMode();
            }
        }
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
}
