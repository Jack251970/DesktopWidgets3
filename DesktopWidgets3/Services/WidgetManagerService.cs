using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Models.Widget;
using DesktopWidgets3.Views.Windows;
using Microsoft.UI.Xaml;
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
    private UIElement? currentTitleBar = null;

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
        foreach (var widget in widgetList)
        {
            if (widget.IsEnabled)
            {
                var widgetType = (WidgetType)Enum.Parse(typeof(WidgetType), widget.Type);
                await ShowWidget(widgetType, widget.IndexTag);
                if (TimerWidgets.Contains(widgetType))
                {
                    enableTimer = true;
                }
            }
        }
        if (enableTimer)
        {
            _timersService.StartUpdateTimeTimer();
        }
    }

    public async Task ShowWidget(WidgetType widgetType, int? indexTag)
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
        var blankWindow = new BlankWindow(widgetType, currentIndexTag);
        WidgetsList.Add(blankWindow);
        _ = _activationService.ActivateWidgetWindowAsync(blankWindow);
        blankWindow.InitializeTitleBar(currentTitleBar);
        SetEditMode(blankWindow, false);

        // set window size and position
        WindowExtensions.SetWindowSize(blankWindow, widget.Size.Width, widget.Size.Height);
        if (widget.Position.X != -1 && widget.Position.Y != -1)
        {
            WindowExtensions.Move(blankWindow, widget.Position.X, widget.Position.Y);
        }

        // show window
        blankWindow.Show();

        // enable timer if needed
        if (TimerWidgets.Contains(widgetType))
        {
            _timersService.StartUpdateTimeTimer();
        }
    }

    public void AddCurrentTitleBar(UIElement titleBar)
    {
        currentTitleBar = titleBar;
    }

    public async Task UpdateAllWidgets()
    {
        List<JsonWidgetItem> widgetList = new();
        foreach (var widgetWindow in WidgetsList)
        {
            var position = widgetWindow.AppWindow.Position;
            var size = new WidgetSize(widgetWindow.AppWindow.Size.Width, widgetWindow.AppWindow.Size.Height);
            var widget = new JsonWidgetItem()
            {
                Type = widgetWindow.WidgetType.ToString(),
                IndexTag = widgetWindow.IndexTag,
                IsEnabled = false,
                Position = position,
                Size = size,
            };
            widgetList.Add(widget);
        }
        await _appSettingsService.UpdateWidgetsList(widgetList);
    }

    public async Task CloseWidget(WidgetType widgetType, int indexTag)
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

    public void CloseAllWidgets()
    {
        foreach (var widgetWindow in WidgetsList)
        {
            SetEditMode(widgetWindow, false);
            widgetWindow.Close();
            WidgetsList.Clear();
        }
    }

    public WidgetType GetWidgetType()
    {
        return currentWidgetType;
    }

    public BlankWindow GetWidgetWindow()
    {
        return GetWidgetWindow(currentWidgetType, currentIndexTag)!;
    }

    public async Task<List<DashboardWidgetItem>> GetDashboardWidgetItemsAsync()
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

    private void SetEditMode(BlankWindow window, bool isEditMode)
    {
        window.IsResizable = isEditMode;
    }
}
