using H.NotifyIcon;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Windows.Graphics;

namespace DesktopWidgets3.Services.Widgets;

internal class WidgetManagerService(IAppSettingsService appSettingsService, INavigationService navigationService, ISystemInfoService systemInfoService, IWidgetResourceService widgetResourceService) : IWidgetManagerService
{
    private readonly List<WidgetWindow> WidgetsList = [];

    private readonly IAppSettingsService _appSettingsService = appSettingsService;
    private readonly INavigationService _navigationService = navigationService;
    private readonly ISystemInfoService _systemInfoService = systemInfoService;
    private readonly IWidgetResourceService _widgetResourceService = widgetResourceService;

    private WidgetType currentWidgetType;
    private int currentIndexTag = -1;

    #region widget window

    public async Task Initialize()
    {
        // enable all enabled widgets
        var widgetList = await _appSettingsService.GetWidgetsList();
        foreach (var widget in widgetList)
        {
            if (widget.IsEnabled)
            {
                await CreateWidgetWindow(widget);
            }
        }

        // initialize edit mode overlay window
        EditModeOverlayWindow ??= await WindowsExtensions.GetWindow<OverlayWindow>(WindowsExtensions.ActivationType.Overlay);
        (EditModeOverlayWindow.Content as Frame)?.Navigate(typeof(EditModeOverlayPage));
    }

    public async Task AddWidget(WidgetType widgetType)
    {
        var widgetList = await _appSettingsService.GetWidgetsList();

        // find index tag
        var indexTags = widgetList.Where(x => x.Type == widgetType).Select(x => x.IndexTag).ToList();
        indexTags.Sort();
        var indexTag = 0;
        foreach (var tag in indexTags)
        {
            if (tag != indexTag)
            {
                break;
            }
            indexTag++;
        }

        // save widget item
        var widget = new JsonWidgetItem()
        {
            Type = widgetType,
            IndexTag = indexTag,
            IsEnabled = true,
            Position = new PointInt32(-1, -1),
            Size = WidgetResourceService.GetDefaultSize(widgetType),
            DisplayMonitor = new(GetMonitorInfo(null)),
            Settings = WidgetResourceService.GetDefaultSettings(widgetType),
        };
        await _appSettingsService.UpdateWidgetsList(widget);

        // create widget window
        await CreateWidgetWindow(widget);
    }

    public async Task EnableWidget(WidgetType widgetType, int indexTag)
    {
        var widgetList = await _appSettingsService.GetWidgetsList();

        // find target widget
        var widget = widgetList.FirstOrDefault(x => x.Type == widgetType && x.IndexTag == indexTag);

        if (widget != null && !widget.IsEnabled)
        {
            // update widget item
            widget.IsEnabled = true;
            await _appSettingsService.UpdateWidgetsList(widget);

            // create widget window
            await CreateWidgetWindow(widget);
        }
    }

    public async Task DisableWidget(WidgetType widgetType, int indexTag)
    {
        var widgetWindow = GetWidgetWindow(widgetType, indexTag);
        if (widgetWindow != null)
        {
            var widgetList = await _appSettingsService.GetWidgetsList();

            // find target widget
            var widget = widgetList.FirstOrDefault(x => x.Type == widgetType && x.IndexTag == indexTag);

            if (widget != null && widget.IsEnabled)
            {
                // update widget item
                widget.IsEnabled = false;
                await _appSettingsService.UpdateWidgetsList(widget);

                // close widget window
                await CloseWidgetWindow(widgetWindow);

                // stop monitor and timer if no more widgets of this type
                CheckMonitorAndTimer(widgetType);
            }
        }
    }

    public async Task DeleteWidget(WidgetType widgetType, int indexTag)
    {
        var widgetList = await _appSettingsService.GetWidgetsList();

        // find target widget
        var widget = widgetList.FirstOrDefault(x => x.Type == widgetType && x.IndexTag == indexTag);

        // update widget item
        widget ??= new JsonWidgetItem()
        {
            Type = widgetType,
            IndexTag = indexTag,
            Position = new PointInt32(-1, -1),
            Size = WidgetResourceService.GetDefaultSize(widgetType),
            DisplayMonitor = new(),
            Settings = WidgetResourceService.GetDefaultSettings(widgetType),
        };
        await _appSettingsService.DeleteWidgetsList(widget);

        var widgetWindow = GetWidgetWindow(widgetType, indexTag);
        if (widgetWindow != null)
        {
            // close widget window
            await CloseWidgetWindow(widgetWindow);

            // stop monitor and timer if no more widgets of this type
            CheckMonitorAndTimer(widgetType);
        }
    }

    public async Task DisableAllWidgets()
    {
        var widgetsList = new List<WidgetWindow>(WidgetsList);
        foreach (var widgetWindow in widgetsList)
        {
            // close widget window
            await CloseWidgetWindow(widgetWindow);
        }

        // stop all monitors
        foreach (WidgetType widgetType in Enum.GetValues(typeof(WidgetType)))
        {
            _systemInfoService.StopMonitor(widgetType);
        }
    }

    public bool IsWidgetEnabled(WidgetType widgetType, int indexTag)
    {
        return GetWidgetWindow(widgetType, indexTag) != null;
    }

    private async Task CreateWidgetWindow(JsonWidgetItem widget)
    {
        // load widget info
        currentWidgetType = widget.Type;
        currentIndexTag = widget.IndexTag;

        // configure widget window lifecycle actions
        var minSize = _widgetResourceService.GetMinSize(currentWidgetType);
        var lifecycleActions = new WindowLifecycleActions()
        {
            Window_Created = (window) => WidgetWindow_Created(window, widget, minSize),
            Window_Closing = WidgetWindow_Closing
        };

        // create widget window
        var newThread = _widgetResourceService.GetWidgetInNewThread(currentWidgetType);
        var widgetWindow = await WindowsExtensions.GetWindow<WidgetWindow>(WindowsExtensions.ActivationType.Widget, widget.Settings, newThread, lifecycleActions);

        // handle monitor
        _systemInfoService.StartMonitor(widget.Type);

        // add to widget list
        WidgetsList.Add(widgetWindow);
    }

    // created action for widget window lifecycle
    private void WidgetWindow_Created(Window window, JsonWidgetItem widget, RectSize minSize)
    {
        if (window is WidgetWindow widgetWindow)
        {
            // initialize widget settings
            widgetWindow.InitializeSettings(widget);

            // set window style, size and position
            widgetWindow.IsResizable = false;
            widgetWindow.MinSize = minSize;
            widgetWindow.Size = widget.Size;
            if (widget.Position.X != -1 && widget.Position.Y != -1)
            {
                widgetWindow.Position = widget.Position;
            }

            // initialize window
            widgetWindow.InitializeWindow();

            // register right tapped menu
            RegisterRightTappedMenu(widgetWindow);

            // show window
            widgetWindow.Show(true);
        }
    }

    #region right tapped menu

    private MenuFlyout RightTappedMenu => GetRightTappedMenu();

    private void RegisterRightTappedMenu(WidgetWindow widgetWindow)
    {
        var page = widgetWindow.FramePage;
        if (page is IWidgetMenu menuPage)
        {
            var element = menuPage.GetWidgetMenuFrameworkElement();
            if (element != null)
            {
                element.RightTapped += ShowRightTappedMenu;
                return;
            }
        }
        page.RightTapped += ShowRightTappedMenu;
    }

    private void ShowRightTappedMenu(object sender, RightTappedRoutedEventArgs e)
    {
        var element = sender as FrameworkElement;
        if (element != null)
        {
            RightTappedMenu.ShowAt(element, new FlyoutShowOptions { Position = e.GetPosition(element) });
            e.Handled = true;
        }
    }

    private MenuFlyout GetRightTappedMenu()
    {
        var menuFlyout = new MenuFlyout();
        var disableMenuItem = new MenuFlyoutItem
        {
            Text = "MenuFlyoutItem_DisableWidget_Text".GetLocalized()
        };
        disableMenuItem.Click += (s, e) => DisableWidget();
        menuFlyout.Items.Add(disableMenuItem);

        var deleteMenuItem = new MenuFlyoutItem
        {
            Text = "MenuFlyoutItem_DeleteWidget_Text".GetLocalized()
        };
        deleteMenuItem.Click += (s, e) => DeleteWidget();
        menuFlyout.Items.Add(deleteMenuItem);

        menuFlyout.Items.Add(new MenuFlyoutSeparator());

        var enterMenuItem = new MenuFlyoutItem
        {
            Text = "MenuFlyoutItem_EnterEditMode_Text".GetLocalized()
        };
        enterMenuItem.Click += (s, e) => EnterEditMode();
        menuFlyout.Items.Add(enterMenuItem);

        return menuFlyout;
    }

    private async void DisableWidget()
    {
        // TODO: Cache right tapped menu here.
        var widgetWindow = new WidgetWindow();
        var widgetType = widgetWindow.WidgetType;
        var indexTag = widgetWindow.IndexTag;
        await DisableWidget(widgetType, indexTag);
        var parameter = new Dictionary<string, object>
        {
            { "UpdateEvent", DashboardViewModel.UpdateEvent.Disable },
            { "WidgetType", widgetWindow.WidgetType },
            { "IndexTag", widgetWindow.IndexTag }
        };
        RefreshDashboardPage(parameter);
    }

    private async void DeleteWidget()
    {
        // TODO: Cache right tapped menu here.
        var widgetWindow = new WidgetWindow();
        if (await widgetWindow.ShowDeleteWidgetDialog() == WidgetDialogResult.Left)
        {
            var widgetType = widgetWindow.WidgetType;
            var indexTag = widgetWindow.IndexTag;
            await DeleteWidget(widgetType, indexTag);
            var parameter = new Dictionary<string, object>
            {
                { "UpdateEvent", DashboardViewModel.UpdateEvent.Delete },
                { "WidgetType", widgetWindow.WidgetType },
                { "IndexTag", widgetWindow.IndexTag }
            };
            RefreshDashboardPage(parameter);
        }
    }

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

    private async Task CloseWidgetWindow(WidgetWindow widgetWindow)
    {
        // close window
        await WindowsExtensions.CloseWindow(widgetWindow);

        // remove from widget list
        WidgetsList.Remove(widgetWindow);
    }

    // closing action for widget window lifecycle
    private static async void WidgetWindow_Closing(Window window)
    {
        if (window is WidgetWindow widgetWindow)
        {
            // set edit mode
            await widgetWindow.SetEditMode(false);

            // widget close event
            if (widgetWindow.PageViewModel is IWidgetClose viewModel)
            {
                viewModel.WidgetWindow_Closing();
            }
        }
    }

    private void CheckMonitorAndTimer(WidgetType widgetType)
    {
        var sameTypeWidgets = WidgetsList.Count(x => x.WidgetType == widgetType);
        if (sameTypeWidgets == 0)
        {
            _systemInfoService.StopMonitor(widgetType);
        }
    }

    private WidgetWindow? GetWidgetWindow(WidgetType widgetType, int indexTag)
    {
        foreach (var widgetWindow in WidgetsList)
        {
            if (widgetWindow.WidgetType == widgetType && widgetWindow.IndexTag == indexTag)
            {
                return widgetWindow;
            }
        }
        return null;
    }

    #endregion

    #region widget settings

    public async Task<BaseWidgetSettings?> GetWidgetSettings(WidgetType widgetType, int indexTag)
    {
        var widgetList = await _appSettingsService.GetWidgetsList();
        var widget = widgetList.FirstOrDefault(x => x.Type == widgetType && x.IndexTag == indexTag);
        return widget?.Settings.Clone();
    }

    public async Task UpdateWidgetSettings(WidgetType widgetType, int indexTag, BaseWidgetSettings settings)
    {
        var widgetWindow = GetWidgetWindow(widgetType, indexTag);
        if (widgetWindow != null)
        {
            await widgetWindow.EnqueueOrInvokeAsync((window) =>
            {
                widgetWindow?.ShellPage?.ViewModel.WidgetNavigationService.NavigateTo(widgetType, new WidgetNavigationParameter()
                {
                    Window = widgetWindow,
                    Settings = settings
                });
            });
        }

        var widgetList = await _appSettingsService.GetWidgetsList();
        var widget = widgetList.FirstOrDefault(x => x.Type == widgetType && x.IndexTag == indexTag);
        if (widget != null)
        {
            widget.Settings = settings;
            await _appSettingsService.UpdateWidgetsList(widget);
        }
    }

    #endregion

    #region dashboard

    public List<DashboardWidgetItem> GetAllWidgetItems()
    {
        List<DashboardWidgetItem> dashboardItemList = [];

        foreach (WidgetType widgetType in Enum.GetValues(typeof(WidgetType)))
        {
            dashboardItemList.Add(new DashboardWidgetItem()
            {
                Type = widgetType,
                IndexTag = 0,
                Label = _widgetResourceService.GetWidgetLabel(widgetType),
                Icon = _widgetResourceService.GetWidgetIconSource(widgetType),
            });
        }

        return dashboardItemList;
    }

    public async Task<List<DashboardWidgetItem>> GetYourWidgetItemsAsync()
    {
        var widgetList = await _appSettingsService.GetWidgetsList();

        List<DashboardWidgetItem> dashboardItemList = [];
        foreach (var widget in widgetList)
        {
            var widgetType = widget.Type;
            dashboardItemList.Add(new DashboardWidgetItem()
            {
                Type = widgetType,
                IndexTag = widget.IndexTag,
                Label = _widgetResourceService.GetWidgetLabel(widgetType),
                IsEnabled = widget.IsEnabled,
                Icon = _widgetResourceService.GetWidgetIconSource(widgetType),
            });
        }

        return dashboardItemList;
    }

    public DashboardWidgetItem GetCurrentEnabledWidget()
    {
        return new DashboardWidgetItem()
        {
            Type = currentWidgetType,
            IndexTag = currentIndexTag,
            IsEnabled = true,
            Label = _widgetResourceService.GetWidgetLabel(currentWidgetType),
            Icon = _widgetResourceService.GetWidgetIconSource(currentWidgetType),
        };
    }

    #endregion

    #region edit mode

    private const int EditModeOverlayWindowXamlWidth = 136;
    private const int EditModeOverlayWindowXamlHeight = 48;

    private OverlayWindow EditModeOverlayWindow = null!;
    private readonly List<JsonWidgetItem> originalWidgetList = [];
    private bool restoreMainWindow = false;

    public async void EnterEditMode()
    {
        // save original widget list
        originalWidgetList.Clear();
        foreach (var widgetWindow in WidgetsList)
        {
            var widget = new JsonWidgetItem()
            {
                Type = widgetWindow.WidgetType,
                IndexTag = widgetWindow.IndexTag,
                IsEnabled = true,
                Position = widgetWindow.Position,
                Size = widgetWindow.Size,
                DisplayMonitor = new(GetMonitorInfo(widgetWindow)),
                Settings = widgetWindow.Settings,
            };
            originalWidgetList.Add(widget);
        }

        // set edit mode for all widgets
        await WidgetsList.EnqueueOrInvokeAsync(async (window) => await window.SetEditMode(true));

        // hide main window if visible
        if (App.MainWindow.Visible)
        {
            await App.MainWindow.EnqueueOrInvokeAsync(WindowsExtensions.CloseWindow);
            restoreMainWindow = true;
        }

        // get primary monitor info & show edit mode overlay window
        var primaryMonitorInfo = MonitorInfo.GetDisplayMonitors().First();
        var screenWidth = primaryMonitorInfo.RectWork.Width;
        await EditModeOverlayWindow.EnqueueOrInvokeAsync((window) =>
        {
            // set window size according to xaml, rember larger than 136 x 39
            EditModeOverlayWindow.Size = new SizeInt32(EditModeOverlayWindowXamlWidth, EditModeOverlayWindowXamlHeight);

            // move to center top
            var windowWidth = EditModeOverlayWindow.AppWindow.Size.Width;
            EditModeOverlayWindow.Position = new PointInt32((int)((screenWidth - windowWidth) / 2), 0);

            // show edit mode overlay window
            EditModeOverlayWindow.Show(true);
        });
    }

    public async void SaveAndExitEditMode()
    {
        // restore edit mode for all widgets
        await WidgetsList.EnqueueOrInvokeAsync(async (window) => await window.SetEditMode(false));

        // hide edit mode overlay window
        EditModeOverlayWindow?.Hide(true);

        // restore main window if needed
        if (restoreMainWindow)
        {
            App.MainWindow.Show();
            restoreMainWindow = false;
        }

        // save widget list
        await Task.Run(async () =>
        {
            List<JsonWidgetItem> widgetList = [];
            foreach (var widgetWindow in WidgetsList)
            {
                var widget = new JsonWidgetItem()
                {
                    Type = widgetWindow.WidgetType,
                    IndexTag = widgetWindow.IndexTag,
                    IsEnabled = true,
                    Position = widgetWindow.Position,
                    Size = widgetWindow.Size,
                    DisplayMonitor = new(GetMonitorInfo(widgetWindow)),
                    Settings = widgetWindow.Settings,
                };
                widgetList.Add(widget);
            }
            await _appSettingsService.UpdateWidgetsList(widgetList);
        });
    }

    public async void CancelAndExitEditMode()
    {
        // restore position, size, edit mode for all widgets
        await WidgetsList.EnqueueOrInvokeAsync(async (window) =>
        {
            // set edit mode for all widgets
            await window.SetEditMode(false);

            // read original position and size
            var originalWidget = originalWidgetList.First(x => x.Type == window.WidgetType && x.IndexTag == window.IndexTag);

            // restore position and size
            if (originalWidget != null)
            {
                window.Position = originalWidget.Position;
                window.Size = originalWidget.Size;
                window.Show(true);
            };
        });

        // hide edit mode overlay window
        EditModeOverlayWindow?.Hide(true);

        // restore main window if needed
        if (restoreMainWindow)
        {
            App.MainWindow.Show();
            restoreMainWindow = false;
        }
    }

    #endregion

    #region navigation

    public void WidgetNavigateTo(WidgetType widgetType, int indexTag, object? parameter = null, bool clearNavigation = false)
    {
        var widgetWindow = GetWidgetWindow(widgetType, indexTag);
        widgetWindow?.ShellPage?.ViewModel.WidgetNavigationService.NavigateTo(widgetType, parameter, clearNavigation);
    }

    #endregion

    #region monitor

    public static MonitorInfo GetMonitorInfo(WindowEx? window)
    {
        return MonitorInfo.GetDisplayMonitors().First();
        // TODO: get monitor info here.
        /*if (window is null)
        {
            var primaryMonitorInfo = MonitorInfo.GetDisplayMonitors().First();
            return primaryMonitorInfo;
        }
        else
        {
            
        }*/
    }

    #endregion
}
