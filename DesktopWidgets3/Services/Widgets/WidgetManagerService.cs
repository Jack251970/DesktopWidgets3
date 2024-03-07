using H.NotifyIcon;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;

namespace DesktopWidgets3.Services.Widgets;

internal class WidgetManagerService : IWidgetManagerService
{
    private readonly List<WidgetWindow> WidgetsList = new();

    private readonly IAppSettingsService _appSettingsService;
    private readonly ISystemInfoService _systemInfoService;
    private readonly ITimersService _timersService;
    private readonly IWidgetResourceService _widgetResourceService;

    private WidgetType currentWidgetType;
    private int currentIndexTag = -1;

    public WidgetManagerService(IAppSettingsService appSettingsService, ISystemInfoService systemInfoService, ITimersService timersService, IWidgetResourceService widgetResourceService)
    {
        _appSettingsService = appSettingsService;
        _systemInfoService = systemInfoService;
        _timersService = timersService;
        _widgetResourceService = widgetResourceService;
    }

    #region widget window

    public async Task EnableAllEnabledWidgets()
    {
        var widgetList = await _appSettingsService.GetWidgetsList();
        foreach (var widget in widgetList)
        {
            if (widget.IsEnabled)
            {
                await CreateWidgetWindow(widget);
            }
        }
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
            Size = _widgetResourceService.GetDefaultSize(widgetType),
            Settings = _widgetResourceService.GetDefaultSettings(widgetType),
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
            Size = _widgetResourceService.GetDefaultSize(widgetType),
            Settings = _widgetResourceService.GetDefaultSettings(widgetType),
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

        // stop all monitors and timers
        foreach (WidgetType widgetType in Enum.GetValues(typeof(WidgetType)))
        {
            _timersService.StopTimer(widgetType);
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
        var liftcycleActions = new WindowLifecycleActions()
        {
            Window_Created = (window) => WidgetWindow_Created(window, widget, minSize),
            Window_Closing = WidgetWindow_Closing
        };

        // create widget window
        var newThread = _widgetResourceService.GetWidgetInNewThread(currentWidgetType);
        var widgetWindow = await UIElementExtensions.GetWindow<WidgetWindow>(ActivationType.Widget, widget.Settings, newThread, liftcycleActions);

        // handle monitor
        _systemInfoService.StartMonitor(widget.Type);

        // handle timer
        _timersService.StartTimer(widget.Type);

        // add to widget list
        WidgetsList.Add(widgetWindow);
    }

    // created action for widget window lifecycle
    private static void WidgetWindow_Created(Window window, JsonWidgetItem widget, WidgetSize minSize)
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

            // show window
            widgetWindow.Show(true);
        }
    }

    private async Task CloseWidgetWindow(WidgetWindow widgetWindow)
    {
        // close window
        await UIElementExtensions.CloseWindow(widgetWindow);

        // remove from widget list
        WidgetsList.Remove(widgetWindow);
    }

    // closing action for widget window lifecycle
    private static async void WidgetWindow_Closing(Window window)
    {
        if (window is WidgetWindow widgetWindow)
        {
            // set edit mode
            await SetEditMode(widgetWindow, false);

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
            _timersService.StopTimer(widgetType);
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
        return widget?.Settings;
    }

    public async Task UpdateWidgetSettings(WidgetType widgetType, int indexTag, BaseWidgetSettings settings)
    {
        var widgetWindow = GetWidgetWindow(widgetType, indexTag);
        widgetWindow?.ShellPage?.ViewModel.WidgetNavigationService.NavigateTo(widgetType, new WidgetNavigationParameter()
        {
            Settings = settings.Clone()
        });

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
        List<DashboardWidgetItem> dashboardItemList = new();

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

        List<DashboardWidgetItem> dashboardItemList = new();
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

    private OverlayWindow? EditModeOverlayWindow
    {
        get; set;
    }

    private readonly List<JsonWidgetItem> originalWidgetList = new();
    private bool restoreMainWindow = false;

    public async void EnterEditMode()
    {
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
                Settings = widgetWindow.Settings,
            };
            originalWidgetList.Add(widget);

            await SetEditMode(widgetWindow, true);
        }

        if (EditModeOverlayWindow == null)
        {
            EditModeOverlayWindow = await UIElementExtensions.GetWindow<OverlayWindow>(ActivationType.Overlay);

            var _shell = EditModeOverlayWindow.Content as Frame;
            _shell?.Navigate(typeof(EditModeOverlayPage));
        }

        // set window size according to xaml, rember larger than 136 x 39
        var xamlWidth = 136;
        var xamlHeight = 48;
        EditModeOverlayWindow.Size = new SizeInt32(xamlWidth, xamlHeight);

        // move to center top
        var primaryMonitorInfo = MonitorInfo.GetDisplayMonitors().First();
        var screenWidth = primaryMonitorInfo.RectWork.Width;
        var windowWidth = EditModeOverlayWindow.AppWindow.Size.Width;
        EditModeOverlayWindow.Position = new PointInt32((int)((screenWidth - windowWidth) / 2), 0);

        EditModeOverlayWindow.Show(true);

        if (App.MainWindow.Visible)
        {
            await UIElementExtensions.CloseWindow(App.MainWindow);
            restoreMainWindow = true;
        }
    }

    public async void SaveAndExitEditMode()
    {
        List<JsonWidgetItem> widgetList = new();

        foreach (var widgetWindow in WidgetsList)
        {
            await SetEditMode(widgetWindow, false);

            var widget = new JsonWidgetItem()
            {
                Type = widgetWindow.WidgetType,
                IndexTag = widgetWindow.IndexTag,
                IsEnabled = true,
                Position = widgetWindow.Position,
                Size = widgetWindow.Size,
                Settings = widgetWindow.Settings,
            };
            widgetList.Add(widget);
        }

        EditModeOverlayWindow?.Hide(true);

        await _appSettingsService.UpdateWidgetsList(widgetList);

        if (restoreMainWindow)
        {
            App.MainWindow.Show(true);
            restoreMainWindow = false;
        }
    }

    public async void CancelAndExitEditMode()
    {
        foreach (var widgetWindow in WidgetsList)
        {
            await SetEditMode(widgetWindow, false);

            var originalWidget = originalWidgetList.First(x => x.Type == widgetWindow.WidgetType && x.IndexTag == widgetWindow.IndexTag);

            if (originalWidget != null)
            {
                widgetWindow.Position = originalWidget.Position;
                widgetWindow.Size = originalWidget.Size;

                widgetWindow.Show(true);
            }
        }

        EditModeOverlayWindow?.Hide(true);

        if (restoreMainWindow)
        {
            App.MainWindow.Show(true);
            restoreMainWindow = false;
        }
    }

    private static async Task SetEditMode(WidgetWindow window, bool isEditMode)
    {
        // set window style
        window.IsResizable = isEditMode;

        // set title bar
        var frameShellPage = window.Content as FrameShellPage;
        frameShellPage!.SetCustomTitleBar(isEditMode);

        // set page update status
        if (window.PageViewModel is IWidgetUpdate viewModel)
        {
            await viewModel.EnableUpdate(!isEditMode);
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
}
