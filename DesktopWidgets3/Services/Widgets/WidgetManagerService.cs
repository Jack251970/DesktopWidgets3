using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Graphics;

namespace DesktopWidgets3.Services.Widgets;

internal class WidgetManagerService(IActivationService activationService, IAppSettingsService appSettingsService, INavigationService navigationService, IWidgetResourceService widgetResourceService) : IWidgetManagerService
{
    private readonly IActivationService _activationService = activationService;
    private readonly IAppSettingsService _appSettingsService = appSettingsService;
    private readonly INavigationService _navigationService = navigationService;
    private readonly IWidgetResourceService _widgetResourceService = widgetResourceService;

    private readonly Dictionary<string, (string, string, int)> PinnedWidgetRuntimeIds = [];
    private readonly Dictionary<string, (string, string)> WidgetSettingRuntimeIds = [];

    private readonly List<WidgetWindowPair> PinnedWidgetWindowPairs = [];
    private readonly List<WidgetSettingPair> WidgetSettingPairs = [];

    private readonly List<JsonWidgetItem> _originalWidgetList = [];
    private bool _inEditMode = false;
    private bool _restoreMainWindow = false;

    #region Widget Info

    #region Runtime Id

    public (string widgetId, string widgetType, int widgetIndex) GetWidgetInfo(string widgetRuntimeId)
    {
        if (PinnedWidgetRuntimeIds.TryGetValue(widgetRuntimeId, out var value))
        {
            return value;
        }

        return (string.Empty, string.Empty, -1);
    }

    private string GetWidgetRuntimeId(string widgetId, string widgetType, int widgetIndex)
    {
        foreach (var (widgetRuntimeId, widgetInfo) in PinnedWidgetRuntimeIds)
        {
            if (widgetInfo == (widgetId, widgetType, widgetIndex))
            {
                return widgetRuntimeId;
            }
        }

        return string.Empty;
    }

    #endregion

    #region Widget Info & Context

    public WidgetInfo? GetWidgetInfo(string widgetId, string widgetType, int widgetIndex)
    {
        // get widget window pair index
        var widgetWindowPairIndex = GetWidgetWindowPairIndex(widgetId, widgetType, widgetIndex);

        // get widget info
        if (widgetWindowPairIndex != -1)
        {
            return PinnedWidgetWindowPairs[widgetWindowPairIndex].WidgetInfo;
        }

        return null;
    }

    public WidgetContext? GetWidgetContext(string widgetId, string widgetType, int widgetIndex)
    {
        // get widget info
        var widgetInfo = GetWidgetInfo(widgetId, widgetType, widgetIndex);

        // get widget context
        if (widgetInfo != null)
        {
            return (WidgetContext)widgetInfo.WidgetContext;
        }

        return null;
    }

    private int GetWidgetWindowPairIndex(string widgetId, string widgetType, int widgetIndex)
    {
        // get widget runtime id
        var widgetRuntimeId = GetWidgetRuntimeId(widgetId, widgetType, widgetIndex);

        // get widget pair index
        return GetWidgetWindowPairIndex(widgetRuntimeId);
    }

    private int GetWidgetWindowPairIndex(string widgetRuntimeId)
    {
        // get widget pair index
        if (widgetRuntimeId != string.Empty)
        {
            return PinnedWidgetWindowPairs.FindIndex(x => x.RuntimeId == widgetRuntimeId);
        }

        return -1;
    }

    #endregion

    #region Is Active

    public bool GetWidgetIsActive(string widgetId, string widgetType, int widgetIndex)
    {
        // get widget window pair index
        var widgetWindowPairIndex = GetWidgetWindowPairIndex(widgetId, widgetType, widgetIndex);

        // get widget is active
        if (widgetWindowPairIndex != -1)
        {
            return PinnedWidgetWindowPairs[widgetWindowPairIndex].Window.IsActive;
        }

        return false;
    }

    #endregion

    #endregion

    #region Widget Setting Info

    #region Setting Info & Runtime Id

    public (string widgetId, string widgetType) GetWidgetSettingInfo(string widgetSettingRuntimeId)
    {
        if (WidgetSettingRuntimeIds.TryGetValue(widgetSettingRuntimeId, out var value))
        {
            return value;
        }

        return (string.Empty, string.Empty);
    }

    private string GetWidgetSettingRuntimeId(string widgetId, string widgetType)
    {
        foreach (var (widgetSettingRuntimeId, widgetInfo) in WidgetSettingRuntimeIds)
        {
            if (widgetInfo == (widgetId, widgetType))
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
        // get widget setting pair index
        var widgetSettingPairIndex = GetWidgetSettingPairIndex(widgetId, widgetType);

        // get widget setting context
        if (widgetSettingPairIndex != -1)
        {
            return WidgetSettingPairs[widgetSettingPairIndex].WidgetSettingContext;
        }

        return null;
    }

    private int GetWidgetSettingPairIndex(string widgetId, string widgetType)
    {
        // get widget setting runtime id
        var widgetSettingRuntimeId = GetWidgetSettingRuntimeId(widgetId, widgetType);

        // get widget setting pair index
        if (widgetSettingRuntimeId != string.Empty)
        {
            return WidgetSettingPairs.FindIndex(x => x.RuntimeId == widgetSettingRuntimeId);
        }

        return -1;
    }

    #endregion

    #region Is Navigated

    public bool GetWidgetSettingIsNavigated(string widgetId, string widgetType)
    {
        // TODO: Implement GetWidgetSettingIsNavigated
        return true;
    }

    #endregion

    #endregion

    #region Widget Window

    #region All Widgets Management

    public void InitializePinnedWidgets()
    {
        var widgetList = _appSettingsService.GetWidgetsList();
        foreach (var widget in widgetList)
        {
            if (widget.Pinned)
            {
                CreateWidgetWindow(widget);
            }
        }
    }

    public async Task RestartWidgetsAsync()
    {
        // close all widgets
        await CloseAllWidgetsAsync();

        // enable all enabled widgets
        InitializePinnedWidgets();
    }

    public async Task CloseAllWidgetsAsync()
    {
        await GetPinnedWidgetWindows().EnqueueOrInvokeAsync(async (window) => {
            // close window
            await CloseWidgetWindowAsync(window.RuntimeId, CloseEvent.Unpin);
        }, Microsoft.UI.Dispatching.DispatcherQueuePriority.High);
    }

    private List<WidgetWindow> GetPinnedWidgetWindows()
    {
        return PinnedWidgetWindowPairs.Select(x => x.Window).ToList();
    }

    #endregion

    #region Single Widget Management

    #region Public

    public async Task AddWidgetAsync(string widgetId, string widgetType, Action<string, string, int> action, bool updateDashboard)
    {
        var widgetList = _appSettingsService.GetWidgetsList();

        // find index tag
        var indexs = widgetList.Where(x => x.Id == widgetId & x.Type == widgetType).Select(x => x.Index).ToList();
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
        action(widgetId, widgetType, index);

        // create widget item
        var widget = new JsonWidgetItem()
        {
            Name = _widgetResourceService.GetWidgetName(widgetId, widgetType),
            Id = widgetId,
            Type = widgetType,
            Index = index,
            Pinned = true,
            Position = new PointInt32(-10000, -10000),
            Size = _widgetResourceService.GetWidgetDefaultSize(widgetId, widgetType),
            DisplayMonitor = DisplayMonitor.GetPrimaryMonitorInfo(),
            Settings = _widgetResourceService.GetDefaultSettings(widgetId, widgetType),
        };

        // create widget window
        var widgetRuntimeId = CreateWidgetWindow(widget);

        // update dashboard page
        if (updateDashboard)
        {
            RefreshDashboardPage(new DashboardViewModelNavigationParameter()
            {
                Event = DashboardViewModelNavigationParameter.UpdateEvent.Pin,
                Id = widgetId,
                Type = widgetType,
                Index = index
            });
        }

        // save widget item
        await _appSettingsService.AddWidgetAsync(widget);
    }

    public async Task PinWidgetAsync(string widgetId, string widgetType, int widgetIndex, bool refresh)
    {
        // get widget
        var widget = _appSettingsService.GetWidget(widgetId, widgetType, widgetIndex);

        // pin widget
        if (widget != null)
        {
            // create widget window
            CreateWidgetWindow(widget);

            // update widget list
            await _appSettingsService.PinWidgetAsync(widgetId, widgetType, widgetIndex);
        }
        else
        {
            // add widget
            await AddWidgetAsync(widgetId, widgetType, (id, type, tag) => { }, false);
        }

        // refresh dashboard page
        if (refresh)
        {
            RefreshDashboardPage(new DashboardViewModelNavigationParameter()
            {
                Event = DashboardViewModelNavigationParameter.UpdateEvent.Pin,
                Id = widgetId,
                Type = widgetType,
                Index = widgetIndex
            });
        }
    }

    public async Task UnpinWidgetAsync(string widgetId, string widgetType, int widgetIndex, bool refresh)
    {
        // get widget runtime id
        var widgetRuntimeId = GetWidgetRuntimeId(widgetId, widgetType, widgetIndex);

        // close widget window
        await CloseWidgetWindowAsync(widgetRuntimeId, CloseEvent.Unpin);

        // refresh dashboard page
        if (refresh)
        {
            RefreshDashboardPage(new DashboardViewModelNavigationParameter()
            {
                Event = DashboardViewModelNavigationParameter.UpdateEvent.Unpin,
                Id = widgetId,
                Type = widgetType,
                Index = widgetIndex
            });
        }

        // update widget list
        await _appSettingsService.UnpinWidgetAsync(widgetId, widgetType, widgetIndex);
    }

    public async Task DeleteWidgetAsync(string widgetId, string widgetType, int widgetIndex, bool refresh)
    {
        // get widget runtime id
        var widgetRuntimeId = GetWidgetRuntimeId(widgetId, widgetType, widgetIndex);

        // close widget window
        await CloseWidgetWindowAsync(widgetRuntimeId, CloseEvent.Delete);

        // refresh dashboard page
        if (refresh)
        {
            RefreshDashboardPage(new DashboardViewModelNavigationParameter()
            {
                Event = DashboardViewModelNavigationParameter.UpdateEvent.Delete,
                Id = widgetId,
                Type = widgetType,
                Index = widgetIndex
            });
        }

        // update widget list
        await _appSettingsService.DeleteWidgetAsync(widgetId, widgetType, widgetIndex);
    }

    #endregion

    #region Private

    private string CreateWidgetWindow(JsonWidgetItem item)
    {
        // get widget info
        var (widgetId, widgetType, widgetIndex) = (item.Id, item.Type, item.Index);

        // create widget info & register guid
        var widgetRuntimeId = StringUtils.GetRandomWidgetRuntimeId();
        var widgetContext = new WidgetContext(this)
        {
            Id = widgetRuntimeId,
            Type = widgetType,
        };
        var widgetInfo = new WidgetInfo(this)
        {
            WidgetContext = widgetContext
        };

        // add to widget runtime id list & widget list
        PinnedWidgetRuntimeIds.Add(widgetInfo.WidgetContext.Id, (widgetId, widgetType, widgetIndex));
        PinnedWidgetWindowPairs.Add(new WidgetWindowPair()
        {
            RuntimeId = widgetRuntimeId,
            WidgetInfo = widgetInfo,
            Window = null!,
            MenuFlyout = null!
        });

        // configure widget window lifecycle actions
        (var minSize, var maxSize) = _widgetResourceService.GetWidgetMinMaxSize(widgetId, widgetType);
        var lifecycleActions = new WindowsExtensions.WindowLifecycleActions()
        {
            Window_Creating = null,
            Window_Created = (window) => WidgetWindow_Created(widgetInfo, window, item, minSize, maxSize),
            Window_Closing = null,
            Window_Closed = null
        };

        // create widget window
        var widgetWindow = WindowsExtensions.CreateWindow<WidgetWindow>(_appSettingsService.MultiThread, lifecycleActions, item);

        return widgetRuntimeId;
    }

    #region Widget Window Lifecycle

    private async void WidgetWindow_Created(WidgetInfo widgetInfo, Window window, JsonWidgetItem item, RectSize minSize, RectSize maxSize)
    {
        if (window is WidgetWindow widgetWindow)
        {
            // activate window
            await _activationService.ActivateWindowAsync(widgetWindow);

            // get widget id & index tag
            var widgetId = item.Id;
            var widgetType = item.Type;
            var widgetIndex = item.Index;

            // set widget ico & title & framework element
            widgetWindow.ViewModel.WidgetIconPath = _widgetResourceService.GetWidgetIconPath(widgetId, widgetType);
            widgetWindow.ViewModel.WidgetDisplayTitle = _widgetResourceService.GetWidgetName(widgetId, widgetType);

            // initialize window
            var menuFlyout = GetWidgetMenuFlyout(widgetWindow);
            widgetWindow.Initialize(widgetInfo.WidgetContext.Id, item, menuFlyout);

            // set window style, size and position
            widgetWindow.IsResizable = false;
            widgetWindow.MinSize = minSize;
            widgetWindow.MaxSize = maxSize;
            widgetWindow.Size = item.Size;
            WindowExtensions.Move(widgetWindow, -10000, -10000);

            // register load event handler
            widgetWindow.LoadCompleted += WidgetWindow_LoadCompleted;

            // activate window
            widgetWindow.Activate();

            // add to widget pair list
            var widgetWindowPairIndex = GetWidgetWindowPairIndex(widgetId, widgetType, widgetIndex);
            if (widgetWindowPairIndex != -1)
            {
                PinnedWidgetWindowPairs[widgetWindowPairIndex].Window = widgetWindow;
                PinnedWidgetWindowPairs[widgetWindowPairIndex].MenuFlyout = menuFlyout;
            }
        }
    }

    private void WidgetWindow_LoadCompleted(object? sender, WidgetWindow.LoadCompletedEventArgs args)
    {
        if (sender is WidgetWindow widgetWindow)
        {
            // unregister load event handler
            widgetWindow.LoadCompleted -= WidgetWindow_LoadCompleted;

            // parse event agrs
            var widgetRuntimeId = args.WidgetRuntimeId;
            var widgetPosition = args.WidgetPosition;
            var widgetSettings = args.WidgetSettings;

            // get widget info
            var (widgetId, widgetType, widgetIndex) = GetWidgetInfo(widgetRuntimeId);

            // set widget position
            widgetWindow.Position = widgetPosition;

            // get widget framework element
            var widgetContext = GetWidgetContext(widgetId, widgetType, widgetIndex)!;
            var frameworkElement = _widgetResourceService.CreateWidgetContent(widgetId, widgetContext!);

            // set widget framework element
            widgetWindow.ViewModel.WidgetFrameworkElement = frameworkElement;

            // invoke widget event after widget framework element is loaded
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

    #endregion

    private async Task CloseWidgetWindowAsync(string widgetRuntimeId, CloseEvent closeEvent)
    {
        // close widget window
        var widgetWindow = GetWidgetWindow(widgetRuntimeId);
        if (widgetWindow != null)
        {
            // get widget info
            var (widgetId, widgetType, widgetIndex) = GetWidgetInfo(widgetRuntimeId);

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

            // remove from widget runtime id list & widget pair list
            PinnedWidgetRuntimeIds.Remove(widgetRuntimeId);
            var widgetWindowPairIndex = GetWidgetWindowPairIndex(widgetId, widgetType, widgetIndex);
            PinnedWidgetWindowPairs.RemoveAt(widgetWindowPairIndex);
        }
    }

    private enum CloseEvent
    {
        Unpin,
        Delete
    }

    private WidgetWindow? GetWidgetWindow(string widgetRuntimeId)
    {
        var widgetPairIndex = GetWidgetWindowPairIndex(widgetRuntimeId);
        if (widgetPairIndex != -1)
        {
            return PinnedWidgetWindowPairs[widgetPairIndex].Window;
        }

        return null;
    }

    #endregion

    #endregion

    #endregion

    #region Widget Setting

    public void NavigateToWidgetSettingPage(string widgetId, string widgetType, int widgetIndex)
    {
        // navigate to widget setting page
        _navigationService.NavigateTo(typeof(WidgetSettingViewModel).FullName!);

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
                WidgetIndex = widgetIndex,
                WidgetSettingContext = widgetSettingContext,
                WidgetSettingContent = frameworkElement,
            };
            WidgetSettingRuntimeIds.Add(widgetSettingRuntimeId, (widgetId, widgetType));
            WidgetSettingPairs.Add(widgetSettingPair);
        }

        // set widget page & add to list & handle events
        var widgetSettingPage = _navigationService.Frame?.Content as WidgetSettingPage;
        if (widgetSettingPage != null)
        {
            // set widget setting framework element
            widgetSettingPage.ViewModel.WidgetFrameworkElement = widgetSettingPair.WidgetSettingContent;
            var widgetName = _widgetResourceService.GetWidgetName(widgetId, widgetType);
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
        // get widget setting pair index
        var widgetSettingPairIndex = GetWidgetSettingPairIndex(widgetId, widgetType);

        // get widget setting pair
        if (widgetSettingPairIndex != -1)
        {
            WidgetSettingPairs[widgetSettingPairIndex].WidgetIndex = widgetIndex;
            return WidgetSettingPairs[widgetSettingPairIndex];
        }

        return null;
    }

    #endregion

    #region Widget Menu

    private MenuFlyout GetWidgetMenuFlyout(WidgetWindow widgetWindow)
    {
        var menuFlyout = new MenuFlyout
        {
            Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft
        };

        AddUnpinDeleteItemsToWidgetMenu(menuFlyout, widgetWindow);
        menuFlyout.Items.Add(new MenuFlyoutSeparator());
        AddLayoutItemsToWidgetMenu(menuFlyout, widgetWindow);
        menuFlyout.Items.Add(new MenuFlyoutSeparator());
        AddRestartItemsToWidgetMenu(menuFlyout, widgetWindow);

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
            Text = "MenuFlyoutItem_UnpinWidget.Text".GetLocalized(),
            Icon = unpinIcon
        };
        unpinWidgetMenuItem.Click += UnpinWidget;
        menuFlyout.Items.Add(unpinWidgetMenuItem);

        var deleteIcon = new FontIcon()
        {
            Glyph = "\uE74D"
        };
        var deleteWidgetMenuItem = new MenuFlyoutItem
        {
            Tag = widgetWindow,
            Text = "MenuFlyoutItem_DeleteWidget.Text".GetLocalized(),
            Icon = deleteIcon
        };
        deleteWidgetMenuItem.Click += DeleteWidget;
        menuFlyout.Items.Add(deleteWidgetMenuItem);
    }

    private async void UnpinWidget(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem deleteMenuItem && deleteMenuItem?.Tag is WidgetWindow widgetWindow)
        {
            // get widget runtime id
            var widgetRuntimeId = widgetWindow.RuntimeId;

            // get widget info
            var (widgetId, widgetType, widgetIndex) = GetWidgetInfo(widgetRuntimeId);

            // unpin widget
            await UnpinWidgetAsync(widgetId, widgetType, widgetIndex, true);
        }
    }

    private async void DeleteWidget(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem deleteMenuItem && deleteMenuItem?.Tag is WidgetWindow widgetWindow)
        {
            // get widget runtime id
            var widgetRuntimeId = widgetWindow.RuntimeId;

            // get widget info
            var (widgetId, widgetType, widgetIndex) = GetWidgetInfo(widgetRuntimeId);

            // delete widget
            await DialogFactory.ShowDeleteWidgetFullScreenDialogAsync(async () => await DeleteWidgetAsync(widgetId, widgetType, widgetIndex, true));
        }
    }

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
            Text = "MenuFlyoutItem_EditWidgetsLayout.Text".GetLocalized()
        };
        editLayoutMenuItem.Click += EditWidgetsLayout;
        menuFlyout.Items.Add(editLayoutMenuItem);
    }

    private void EditWidgetsLayout(object sender, RoutedEventArgs e)
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
            Text = "MenuFlyoutItem_RestartWidget.Text".GetLocalized(),
            Icon = restartIcon,
        };
        restartWidgetMenuItem.Click += RestartWidget;
        menuFlyout.Items.Add(restartWidgetMenuItem);

        restartIcon = new FontIcon()
        {
            Glyph = "\uE72C"
        };
        var restartWidgetsMenuItem = new MenuFlyoutItem
        {
            Tag = widgetWindow,
            Text = "MenuFlyoutItem_RestartWidgets.Text".GetLocalized(),
            Icon = restartIcon
        };
        restartWidgetsMenuItem.Click += RestartWidgets;
        menuFlyout.Items.Add(restartWidgetsMenuItem);
    }

    private async void RestartWidget(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem deleteMenuItem && deleteMenuItem?.Tag is WidgetWindow widgetWindow)
        {
            var widgetRuntimeId = widgetWindow.RuntimeId;

            await CloseWidgetWindowAsync(widgetRuntimeId, CloseEvent.Unpin);

            var widgetList = _appSettingsService.GetWidgetsList();

            // get widget info
            var (widgetId, widgetType, widgetIndex) = GetWidgetInfo(widgetRuntimeId);
            var widget = widgetList.FirstOrDefault(x => x.Id == widgetId & x.Type == widgetType & x.Index == widgetIndex);
            if (widget != null)
            {
                CreateWidgetWindow(widget);
            }
        }
    }

    private async void RestartWidgets(object sender, RoutedEventArgs e)
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
        var widget = widgetList.FirstOrDefault(x => x.Id == widgetId & x.Type == widgetType & x.Index == widgetIndex);

        return widget?.Settings.Clone();
    }

    public async Task UpdateWidgetSettingsAsync(string widgetId, string widgetType, int widgetIndex, BaseWidgetSettings settings)
    {
        // invoke widget settings changed event
        var widgetWindowPairIndex = GetWidgetWindowPairIndex(widgetId, widgetType, widgetIndex);
        if (widgetWindowPairIndex != -1)
        {
            // if widget window exists
            var widgetWindowPair = PinnedWidgetWindowPairs[widgetWindowPairIndex];
            _widgetResourceService.OnWidgetSettingsChanged(widgetId, new WidgetSettingsChangedArgs()
            {
                WidgetContext = widgetWindowPair.WidgetInfo.WidgetContext,
                Settings = settings,
            });
        }
        var widgetSettingPairIndex = GetWidgetSettingPairIndex(widgetId, widgetType);
        if (widgetSettingPairIndex != -1 && WidgetSettingPairs[widgetSettingPairIndex].WidgetIndex == widgetIndex)
        {
            // if widget setting content exists and current index is the same
            var widgetSettingPair = WidgetSettingPairs[widgetSettingPairIndex];
            _widgetResourceService.OnWidgetSettingsChanged(widgetId, new WidgetSettingsChangedArgs()
            {
                WidgetContext = widgetSettingPair.WidgetSettingContext,
                Settings = settings,
            });
        }

        // update widget list
        await _appSettingsService.UpdateWidgetSettingsAsync(widgetId, widgetType, widgetIndex, settings);
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
            var (widgetId, widgetType, widgetIndex) = GetWidgetInfo(widgetWindow.RuntimeId);
            _originalWidgetList.Add(new JsonWidgetItem()
            {
                Name = _widgetResourceService.GetWidgetName(widgetId, widgetType),
                Id = widgetId,
                Type = widgetType,
                Index = widgetIndex,
                Pinned = true,
                Position = widgetWindow.Position,
                Size = widgetWindow.Size,
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
            var (widgetId, widgetType, widgetIndex) = GetWidgetInfo(widgetWindow.RuntimeId);
            widgetList.Add(new JsonWidgetItem()
            {
                Name = _widgetResourceService.GetWidgetName(widgetId, widgetType),
                Id = widgetId,
                Type = widgetType,
                Index = widgetIndex,
                Pinned = true,
                Position = widgetWindow.Position,
                Size = widgetWindow.Size,
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
            var (widgetId, widgetType, widgetIndex) = GetWidgetInfo(window.RuntimeId);

            // read original position and size
            var originalWidget = _originalWidgetList.FirstOrDefault(x => x.Id == widgetId & x.Type == widgetType & x.Index == widgetIndex);

            // restore position and size
            if (originalWidget != null)
            {
                window.Position = originalWidget.Position;
                window.Size = originalWidget.Size;
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
            App.ShowMainWindow(false);
            if (await DialogFactory.ShowQuitEditModeDialogAsync(App.MainWindow) == WidgetDialogResult.Left)
            {
                await SaveAndExitEditMode();
            }
        }
    }

    #endregion

    #region Dashboard

    private void RefreshDashboardPage(object parameter)
    {
        var dashboardPageKey = typeof(DashboardViewModel).FullName!;
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
