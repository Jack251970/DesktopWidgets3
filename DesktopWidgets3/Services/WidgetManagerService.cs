using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Models.Widget;
using DesktopWidgets3.ViewModels.Pages;
using DesktopWidgets3.Views.Pages;
using DesktopWidgets3.Views.Windows;
using Windows.Graphics;

namespace DesktopWidgets3.Services;

public class WidgetManagerService : IWidgetManagerService
{
    private readonly List<BlankWindow> WidgetsList = new();

    private readonly List<WidgetType> TimerWidgets = new()
    {
        WidgetType.Clock,
        WidgetType.CPU,
        WidgetType.Disk,
        WidgetType.Network,
    };

    private readonly IActivationService _activationService;
    private readonly IAppSettingsService _appSettingsService;
    private readonly IThemeSelectorService _themeSelectorService;
    private readonly ITimersService _timersService;
    private readonly IWidgetResourceService _widgetResourceService;

    private WidgetType currentWidgetType;
    private int currentIndexTag = -1;

    public WidgetManagerService(IActivationService activationService, IAppSettingsService appSettingsService, IThemeSelectorService themeSelectorService, ITimersService timersService, IWidgetResourceService widgetResourceService)
    {
        _activationService = activationService;
        _appSettingsService = appSettingsService;
        _themeSelectorService = themeSelectorService;
        _timersService = timersService;
        _widgetResourceService = widgetResourceService;
    }

    public async Task SetThemeAsync()
    {
        foreach (var widgetWindow in WidgetsList)
        {
            await _themeSelectorService.SetRequestedThemeAsync(widgetWindow);
        }
    }

    public async Task InitializeWidgets()
    {
        var widgetList = await _appSettingsService.GetWidgetsList();
        var enableTimer = false;

        // show widgets
        foreach (var widget in widgetList)
        {
            if (widget.IsEnabled)
            {
                var widgetType = (WidgetType)Enum.Parse(typeof(WidgetType), widget.Type);
                await EnableWidget(widgetType, widget.IndexTag);
                if (TimerWidgets.Contains(widgetType))
                {
                    enableTimer = true;
                }
            }
        }

        // enable timer if needed
        if (enableTimer)
        {
            _timersService.StartUpdateTimeTimer();
        }
    }

    public async Task EnableWidget(WidgetType widgetType, int? indexTag)
    {
        var widgetList = await _appSettingsService.GetWidgetsList();

        // find index tag
        var sameTypeWidgets = widgetList.Where(x => x.Type == widgetType.ToString());
        JsonWidgetItem? widget = null;
        if (indexTag == null)
        {
            if (sameTypeWidgets.Any())
            {
                indexTag = sameTypeWidgets.Max(x => x.IndexTag) + 1;
            }
            else
            {
                indexTag = 0;
            }
        }
        else
        {
            widget = sameTypeWidgets.FirstOrDefault(x => x.IndexTag == indexTag);
        }

        currentWidgetType = widgetType;
        currentIndexTag = (int)indexTag;

        // save widget item
        if (widget == null)
        {
            widget = new JsonWidgetItem()
            {
                Type = widgetType.ToString(),
                IndexTag = currentIndexTag,
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
        var widgetWindow = new BlankWindow(widgetType, currentIndexTag);
        WidgetsList.Add(widgetWindow);
        await _activationService.ActivateWidgetWindowAsync(widgetWindow);
#if DBEUG
        // wait for 1 second to avoid Access Violation exception under debug mode
        await Task.Delay(1000);
#endif
        widgetWindow.IsResizable = false;

        // set window size and position
        WindowExtensions.SetWindowSize(widgetWindow, widget.Size.Width, widget.Size.Height);
        if (widget.Position.X != -1 && widget.Position.Y != -1)
        {
            WindowExtensions.Move(widgetWindow, widget.Position.X, widget.Position.Y);
        }

        // show window
        widgetWindow.Show();

        // enable timer if needed
        if (TimerWidgets.Contains(widgetType))
        {
            _timersService.StartUpdateTimeTimer();
        }
    }

    public async Task DisableWidget(WidgetType widgetType, int indexTag)
    {
        var widgetWindow = GetWidgetWindow(widgetType, indexTag);
        if (widgetWindow != null)
        {
            var widgetList = await _appSettingsService.GetWidgetsList();
            var widget = widgetList.FirstOrDefault(x => x.Type == widgetType.ToString() && x.IndexTag == indexTag);

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
                var position = widgetWindow.AppWindow.Position;
                var size = new WidgetSize(widgetWindow.AppWindow.Size.Width, widgetWindow.AppWindow.Size.Height);
                widget = new JsonWidgetItem()
                {
                    Type = widgetType.ToString(),
                    IndexTag = indexTag,
                    IsEnabled = false,
                    Position = position,
                    Size = size,
                };
                await _appSettingsService.UpdateWidgetsList(widget);
            }

            SetEditMode(widgetWindow, false);
            widgetWindow.Close();
            WidgetsList.Remove(widgetWindow);
        }

        // disable timer if needed
        if (!WidgetsList.Any(x => TimerWidgets.Contains(x.WidgetType)))
        {
            _timersService.StopUpdateTimeTimer();
        }
    }

    public async Task DisableWidget(BlankWindow widgetWindow)
    {
        // invoke from widget window iteself
        var widgetType = widgetWindow.WidgetType;
        var indexTag = widgetWindow.IndexTag;

        await DisableWidget(widgetType, indexTag);

        // TODO: refresh dashboard if needed
    }

    public void CloseAllWidgets()
    {
        foreach (var widgetWindow in WidgetsList)
        {
            SetEditMode(widgetWindow, false);
            widgetWindow.Close();
        }
        WidgetsList.Clear();
    }

    public BlankWindow GetCurrentWidgetWindow()
    {
        return WidgetsList.Last();
    }

    public DashboardWidgetItem GetDashboardWidgetItem()
    {
        return new DashboardWidgetItem()
        {
            Type = currentWidgetType,
            IndexTag = currentIndexTag,
            Label = _widgetResourceService.GetWidgetLabel(currentWidgetType),
            Icon = _widgetResourceService.GetWidgetIconSource(currentWidgetType),
        };
    }

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
            var widgetType = (WidgetType)Enum.Parse(typeof(WidgetType), widget.Type);
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

    public void EnterEditMode()
    {
        foreach (var widgetWindow in WidgetsList)
        {
            SetEditMode(widgetWindow, true);
        }
    }

    public async void ExitEditModeAndSave()
    {
        List<JsonWidgetItem> widgetList = new();

        foreach (var widgetWindow in WidgetsList)
        {
            SetEditMode(widgetWindow, false);

            var position = widgetWindow.AppWindow.Position;
            var size = new WidgetSize(widgetWindow.AppWindow.Size.Width, widgetWindow.AppWindow.Size.Height);
            var widget = new JsonWidgetItem()
            {
                Type = widgetWindow.WidgetType.ToString(),
                IndexTag = widgetWindow.IndexTag,
                IsEnabled = true,
                Position = position,
                Size = size,
            };
            widgetList.Add(widget);
        }

        await _appSettingsService.UpdateWidgetsList(widgetList);
    }

    private BlankWindow? GetWidgetWindow(WidgetType widgetType, int indexTag)
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

    private static void SetEditMode(BlankWindow window, bool isEditMode)
    {
        window.IsResizable = isEditMode;
    }
}
