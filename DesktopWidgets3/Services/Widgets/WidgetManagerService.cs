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

    private string currentWidgetId = StringUtils.GetGuid();
    private int currentIndexTag = -1;

    #region widget window

    public async Task Initialize()
    {
        // initialize widgets
        _widgetResourceService.Initalize();

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

    public async Task AddWidget(string widgetId)
    {
        var widgetList = await _appSettingsService.GetWidgetsList();

        // find index tag
        var indexTags = widgetList.Where(x => x.Id == widgetId).Select(x => x.IndexTag).ToList();
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
            Id = widgetId,
            IndexTag = indexTag,
            IsEnabled = true,
            Position = new PointInt32(-1, -1),
            Size = _widgetResourceService.GetDefaultSize(widgetId),
            DisplayMonitor = new(GetMonitorInfo(null)),
            Settings = _widgetResourceService.GetDefaultSetting(widgetId),
        };
        await _appSettingsService.UpdateWidgetsList(widget);

        // create widget window
        await CreateWidgetWindow(widget);
    }

    public async Task EnableWidget(string widgetId, int indexTag)
    {
        var widgetList = await _appSettingsService.GetWidgetsList();

        // find target widget
        var widget = widgetList.FirstOrDefault(x => x.Id == widgetId && x.IndexTag == indexTag);

        if (widget != null && !widget.IsEnabled)
        {
            // update widget item
            widget.IsEnabled = true;
            await _appSettingsService.UpdateWidgetsList(widget);

            // create widget window
            await CreateWidgetWindow(widget);
        }
    }

    public async Task DisableWidget(string widgetId, int indexTag)
    {
        var widgetWindow = GetWidgetWindow(widgetId, indexTag);
        if (widgetWindow != null)
        {
            var widgetList = await _appSettingsService.GetWidgetsList();

            // find target widget
            var widget = widgetList.FirstOrDefault(x => x.Id == widgetId && x.IndexTag == indexTag);

            if (widget != null && widget.IsEnabled)
            {
                // update widget item
                widget.IsEnabled = false;
                await _appSettingsService.UpdateWidgetsList(widget);

                // close widget window
                await CloseWidgetWindow(widgetWindow);

                // stop monitor and timer if no more widgets of this type
                CheckMonitorAndTimer(widgetId);
            }
        }
    }

    public async Task DeleteWidget(string widgetId, int indexTag)
    {
        var widgetList = await _appSettingsService.GetWidgetsList();

        // find target widget
        var widget = widgetList.FirstOrDefault(x => x.Id == widgetId && x.IndexTag == indexTag);

        // update widget item
        widget ??= new JsonWidgetItem()
        {
            Id = widgetId,
            IndexTag = indexTag,
            Position = new PointInt32(-1, -1),
            Size = _widgetResourceService.GetDefaultSize(widgetId),
            DisplayMonitor = new(),
            Settings = _widgetResourceService.GetDefaultSetting(widgetId),
        };
        await _appSettingsService.DeleteWidgetsList(widget);

        var widgetWindow = GetWidgetWindow(widgetId, indexTag);
        if (widgetWindow != null)
        {
            // close widget window
            await CloseWidgetWindow(widgetWindow);

            // stop monitor and timer if no more widgets of this type
            CheckMonitorAndTimer(widgetId);
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
        foreach (HardwareType hardwareType in Enum.GetValues(typeof(HardwareType)))
        {
            _systemInfoService.StopMonitor(hardwareType);
        }
    }

    public bool IsWidgetEnabled(string widgetId, int indexTag)
    {
        return GetWidgetWindow(widgetId, indexTag) != null;
    }

    private async Task CreateWidgetWindow(JsonWidgetItem widget)
    {
        // load widget info
        currentWidgetId = widget.Id;
        currentIndexTag = widget.IndexTag;

        // configure widget window lifecycle actions
        var minSize = _widgetResourceService.GetMinSize(currentWidgetId);
        var lifecycleActions = new WindowLifecycleActions()
        {
            Window_Created = (window) => WidgetWindow_Created(window, widget, minSize),
            Window_Closing = WidgetWindow_Closing
        };

        // create widget window
        var newThread = _widgetResourceService.GetWidgetInNewThread(currentWidgetId);
        var widgetWindow = await WindowsExtensions.GetWindow<WidgetWindow>(WindowsExtensions.ActivationType.Widget, widget.Settings, newThread, lifecycleActions);

        // TODO: Now handle monitor by widget itself.

        // add to widget list
        WidgetsList.Add(widgetWindow);
    }

    // created action for widget window lifecycle
    private void WidgetWindow_Created(Window window, JsonWidgetItem widgetItem, RectSize minSize)
    {
        if (window is WidgetWindow widgetWindow)
        {
            // initialize widget framework element
            // TODO: Store view model here.
            var element = _widgetResourceService.GetWidgetFrameworkElement(widgetItem.Id);
            var viewModel = element.DataContext;
            widgetWindow.ShellPage.SetFrameworkElement(element);

            // initialize widget window & settings
            widgetWindow.InitializeWindow(widgetItem);

            // initialize widget settings
            if(viewModel is IWidgetNavigation nav)
            {
                nav.UpdateWidgetViewModel(new WidgetNavigationParameter()
                {
                    Window = window,
                    Settings = widgetItem.Settings
                });
            }

            // set window style, size and position
            widgetWindow.IsResizable = false;
            widgetWindow.MinSize = minSize;
            widgetWindow.Size = widgetItem.Size;
            if (widgetItem.Position.X != -1 && widgetItem.Position.Y != -1)
            {
                widgetWindow.Position = widgetItem.Position;
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
        var content = widgetWindow.FrameworkElement;
        if (content is IWidgetMenu menuPage)
        {
            var element = menuPage.GetWidgetMenuFrameworkElement();
            if (element != null)
            {
                element.RightTapped += ShowRightTappedMenu;
                return;
            }
        }
        if (content != null)
        {
            content.RightTapped += ShowRightTappedMenu;
        }
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
        var widgetId = widgetWindow.Id;
        var indexTag = widgetWindow.IndexTag;
        await DisableWidget(widgetId, indexTag);
        var parameter = new Dictionary<string, object>
        {
            { "UpdateEvent", DashboardViewModel.UpdateEvent.Disable },
            { "string", widgetWindow.Id },
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
            var widgetId = widgetWindow.Id;
            var indexTag = widgetWindow.IndexTag;
            await DeleteWidget(widgetId, indexTag);
            var parameter = new Dictionary<string, object>
            {
                { "UpdateEvent", DashboardViewModel.UpdateEvent.Delete },
                { "string", widgetWindow.Id },
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

    private void CheckMonitorAndTimer(string widgetId)
    {
        var sameTypeWidgets = WidgetsList.Count(x => x.Id == widgetId);
        if (sameTypeWidgets == 0)
        {
            // TODO: Stop Monitor there.
            // _systemInfoService.StopMonitor(widgetId);
        }
    }

    private WidgetWindow? GetWidgetWindow(string widgetId, int indexTag)
    {
        foreach (var widgetWindow in WidgetsList)
        {
            if (widgetWindow.Id == widgetId && widgetWindow.IndexTag == indexTag)
            {
                return widgetWindow;
            }
        }
        return null;
    }

    #endregion

    #region widget settings

    public async Task<BaseWidgetSettings?> GetWidgetSettings(string widgetId, int indexTag)
    {
        var widgetList = await _appSettingsService.GetWidgetsList();
        var widget = widgetList.FirstOrDefault(x => x.Id == widgetId && x.IndexTag == indexTag);
        return widget?.Settings.Clone();
    }

    public async Task UpdateWidgetSettings(string widgetId, int indexTag, BaseWidgetSettings settings)
    {
        var widgetWindow = GetWidgetWindow(widgetId, indexTag);
        if (widgetWindow != null)
        {
            await widgetWindow.EnqueueOrInvokeAsync((window) =>
            {
                widgetWindow.UpdatePageViewModel(new WidgetNavigationParameter()
                {
                    Window = widgetWindow,
                    Settings = settings
                });
            });
        }

        var widgetList = await _appSettingsService.GetWidgetsList();
        var widget = widgetList.FirstOrDefault(x => x.Id == widgetId && x.IndexTag == indexTag);
        if (widget != null)
        {
            widget.Settings = settings;
            await _appSettingsService.UpdateWidgetsList(widget);
        }
    }

    #endregion

    #region dashboard

    // TODO: Move these methods to widget resource service.

    public async Task<List<DashboardWidgetItem>> GetYourWidgetItemsAsync()
    {
        var widgetList = await _appSettingsService.GetWidgetsList();

        List<DashboardWidgetItem> dashboardItemList = [];
        foreach (var widget in widgetList)
        {
            var widgetId = widget.Id;
            dashboardItemList.Add(new DashboardWidgetItem()
            {
                Id = widgetId,
                IndexTag = widget.IndexTag,
                Label = _widgetResourceService.GetWidgetLabel(widgetId),
                IsEnabled = widget.IsEnabled,
                Icon = _widgetResourceService.GetWidgetIconSource(widgetId),
            });
        }

        return dashboardItemList;
    }

    public DashboardWidgetItem GetCurrentEnabledWidget()
    {
        return new DashboardWidgetItem()
        {
            Id = currentWidgetId,
            IndexTag = currentIndexTag,
            IsEnabled = true,
            Label = _widgetResourceService.GetWidgetLabel(currentWidgetId),
            Icon = _widgetResourceService.GetWidgetIconSource(currentWidgetId),
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
                Id = widgetWindow.Id,
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
                    Id = widgetWindow.Id,
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
            var originalWidget = originalWidgetList.First(x => x.Id == window.Id && x.IndexTag == window.IndexTag);

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
