using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Contracts.ViewModels;
using DesktopWidgets3.Models.Widget;
using DesktopWidgets3.Views.Pages;
using DesktopWidgets3.Views.Pages.Widget;
using DesktopWidgets3.Views.Windows;
using H.NotifyIcon;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;

namespace DesktopWidgets3.Services;

public class WidgetManagerService : IWidgetManagerService
{
    private readonly List<WidgetWindow> WidgetsList = new();

    private readonly IActivationService _activationService;
    private readonly IAppSettingsService _appSettingsService;
    private readonly ISystemInfoService _systemInfoService;
    private readonly IThemeSelectorService _themeSelectorService;
    private readonly ITimersService _timersService;
    private readonly IWidgetResourceService _widgetResourceService;

    private WidgetType currentWidgetType;
    private int currentIndexTag = -1;

    public WidgetManagerService(IActivationService activationService, IAppSettingsService appSettingsService, ISystemInfoService systemInfoService, IThemeSelectorService themeSelectorService, ITimersService timersService, IWidgetResourceService widgetResourceService)
    {
        _activationService = activationService;
        _appSettingsService = appSettingsService;
        _systemInfoService = systemInfoService;
        _themeSelectorService = themeSelectorService;
        _timersService = timersService;
        _widgetResourceService = widgetResourceService;
    }

    #region theme setting

    public async Task SetThemeAsync()
    {
        foreach (var widgetWindow in WidgetsList)
        {
            await _themeSelectorService.SetRequestedThemeAsync(widgetWindow);
        }
        if (EditModeOverlayWindow != null)
        {
            await _themeSelectorService.SetRequestedThemeAsync(EditModeOverlayWindow);
        }
    }

    #endregion

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

    public async Task EnableWidget(WidgetType widgetType, int? indexTag)
    {
        var widgetList = await _appSettingsService.GetWidgetsList();

        // find index tag
        var sameTypeWidgets = widgetList.Where(x => x.Type == widgetType);
        JsonWidgetItem? widget = null;
        if (indexTag == null)
        {
            var indexTags = sameTypeWidgets.Select(x => x.IndexTag).ToList();
            indexTags.Sort();
            indexTag = 0;
            foreach (var tag in indexTags)
            {
                if (tag != indexTag)
                {
                    break;
                }
                indexTag++;
            }
        }
        else
        {
            widget = sameTypeWidgets.FirstOrDefault(x => x.IndexTag == indexTag);
        }

        // save widget item
        if (widget == null)
        {
            widget = new JsonWidgetItem()
            {
                Type = widgetType,
                IndexTag = (int)indexTag,
                IsEnabled = true,
                Position = new PointInt32(-1, -1),
                Size = _widgetResourceService.GetDefaultSize(widgetType),
            };
            await _appSettingsService.UpdateWidgetsList(widget);
        }
        else
        {
            if (!widget.IsEnabled)
            {
                widget.IsEnabled = true;
                await _appSettingsService.UpdateWidgetsList(widget);
            }
        }

        // create widget window
        await CreateWidgetWindow(widget);
    }

    public async Task DisableWidget(WidgetType widgetType, int indexTag)
    {
        var widgetWindow = GetWidgetWindow(widgetType, indexTag);
        if (widgetWindow != null)
        {
            var widgetList = await _appSettingsService.GetWidgetsList();
            var widget = widgetList.FirstOrDefault(x => x.Type == widgetType && x.IndexTag == indexTag);

            if (widget != null)
            {
                if (widget.IsEnabled)
                {
                    widget.IsEnabled = false;
                    await _appSettingsService.UpdateWidgetsList(widget);
                }
            }
            else
            {
                widget = new JsonWidgetItem()
                {
                    Type = widgetType,
                    IndexTag = indexTag,
                    IsEnabled = false,
                    Position = widgetWindow.Position,
                    Size = widgetWindow.Size,
                };
                await _appSettingsService.UpdateWidgetsList(widget);
            }

            await CloseWidgetWindow(widgetWindow);

            var sameTypeWidgets = WidgetsList.Count(x => x.WidgetType == widgetType);
            if (sameTypeWidgets == 0)
            {
                _timersService.StopTimer(widgetType);
                _systemInfoService.StopMonitor(widgetType);
            }
        }
    }

    public async Task DeleteWidget(WidgetType widgetType, int indexTag)
    {
        var widgetList = await _appSettingsService.GetWidgetsList();
        var widget = widgetList.FirstOrDefault(x => x.Type == widgetType && x.IndexTag == indexTag);

        widget ??= new JsonWidgetItem()
        {
            Type = widgetType,
            IndexTag = indexTag,
            IsEnabled = true,
            Position = new PointInt32(-1, -1),
            Size = _widgetResourceService.GetDefaultSize(widgetType),
        };
        await _appSettingsService.DeleteWidgetsList(widget);

        var widgetWindow = GetWidgetWindow(widgetType, indexTag);
        if (widgetWindow != null)
        {
            await CloseWidgetWindow(widgetWindow);

            var sameTypeWidgets = WidgetsList.Count(x => x.WidgetType == widgetType);
            if (sameTypeWidgets == 0)
            {
                _timersService.StopTimer(widgetType);
                _systemInfoService.StopMonitor(widgetType);
            }
        }
    }

    public async void DisableAllWidgets()
    {
        var widgetsList = new List<WidgetWindow>(WidgetsList);
        foreach (var widgetWindow in widgetsList)
        {
            await CloseWidgetWindow(widgetWindow);
        }

        var allWidgetType = Enum.GetValues(typeof(WidgetType));
        foreach (WidgetType widgetType in allWidgetType)
        {
            _timersService.StopTimer(widgetType);
            _systemInfoService.StopMonitor(widgetType);
        }
    }

    public WidgetWindow GetLastWidgetWindow()
    {
        return WidgetsList.Last();
    }

    private async Task CreateWidgetWindow(JsonWidgetItem widget)
    {
        // Load widget info
        currentWidgetType = widget.Type;
        currentIndexTag = widget.IndexTag;

        // create widget window
        var widgetWindow = new WidgetWindow(widget);
        WidgetsList.Add(widgetWindow);

        // handle widget settings
        await _activationService.ActivateWidgetWindowAsync(widgetWindow, widget.Settings);

        // set window style, size and position
        widgetWindow.IsResizable = false;
        widgetWindow.MinSize = _widgetResourceService.GetMinSize(currentWidgetType);
        widgetWindow.Size = widget.Size;
        if (widget.Position.X != -1 && widget.Position.Y != -1)
        {
            widgetWindow.Position = widget.Position;
        }

        // initialize window
        widgetWindow.InitializeWindow();

        // show window
        widgetWindow.Show(true);

        // handle monitor
        _systemInfoService.StartMonitor(widget.Type);

        // handle timer
        _timersService.StartTimer(widget.Type);
    }

    private async Task CloseWidgetWindow(WidgetWindow widgetWindow)
    {
        // set edit mode
        await SetEditMode(widgetWindow, false);

        // widget close event
        if (widgetWindow.PageViewModel is IWidgetClose viewModel)
        {
            viewModel.WidgetWindow_Closing();
        }

        // close windows
        widgetWindow.Close();

        // remove from widget list
        WidgetsList.Remove(widgetWindow);
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
        widgetWindow?.ShellPage?.ViewModel.WidgetNavigationService.NavigateTo(widgetType, settings.Clone());

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
            };
            originalWidgetList.Add(widget);

            await SetEditMode(widgetWindow, true);
        }

        if (EditModeOverlayWindow == null)
        {
            EditModeOverlayWindow = new OverlayWindow();

            await _activationService.ActivateOverlayWindowAsync(EditModeOverlayWindow);
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
            App.MainWindow.Close();
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
        frameShellPage?.SetCustomTitleBar(isEditMode);

        // set page update status
        if (window.PageViewModel is IWidgetUpdate viewModel)
        {
            await viewModel.EnableUpdate(!isEditMode);
        }
    }

    #endregion
}
