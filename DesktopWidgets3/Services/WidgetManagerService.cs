using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Models.Widget;
using DesktopWidgets3.Views.Windows;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace DesktopWidgets3.Services;

public class WidgetManagerService : IWidgetManagerService
{
    private readonly Dictionary<WidgetType, BlankWindow> WidgetsDict = new();
    private readonly Dictionary<WidgetType, UIElement> TitleBarDict = new();

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

    public WidgetManagerService(IActivationService activationService, IAppSettingsService appSettingsService, IThemeSelectorService themeSelectorService, ITimersService timersService, IWidgetResourceService widgetResourceService)
    {
        _activationService = activationService;
        _appSettingsService = appSettingsService;
        _themeSelectorService = themeSelectorService;
        _timersService = timersService;
        _widgetResourceService = widgetResourceService;
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
                await ShowWidget(widgetType);
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

    public async Task ShowWidget(WidgetType widgetType)
    {
        if (!WidgetsDict.TryGetValue(widgetType, out var widgetWindow))
        {
            var widgetList = await _appSettingsService.GetWidgetsList();
            var widget = widgetList.FirstOrDefault(x => (WidgetType)Enum.Parse(typeof(WidgetType), x.Type) == widgetType);
            if (widget == null)
            {
                widget = new JsonWidgetItem()
                {
                    Type = widgetType.ToString(),
                    IsEnabled = true,
                    Position = new PointInt32(-1, -1),
                    Size = _widgetResourceService.GetDefaultSize(widgetType),
                };
                await _appSettingsService.SaveWidgetsList(widget);
            }
            else
            {
                widget.IsEnabled = true;
                await _appSettingsService.SaveWidgetsList(widget);
            }

            currentWidgetType = widgetType;
            var blankWindow = new BlankWindow();
            WidgetsDict.Add(widgetType, blankWindow);

            _ = _activationService.ActivateWidgetWindowAsync(blankWindow);
            blankWindow.Initialize();

            WindowExtensions.SetWindowSize(blankWindow, widget.Size.Width, widget.Size.Height);
            if (widget.Position.X != -1 && widget.Position.Y != -1)
            {
                WindowExtensions.Move(blankWindow, widget.Position.X, widget.Position.Y);
            }

            blankWindow.Show();
        }
        if (TimerWidgets.Contains(widgetType))
        {
            _timersService.StartUpdateTimeTimer();
        }
    }

    public async Task UpdateWidgetPosition(WidgetType widgetType, PointInt32 position)
    {
        var widgetList = await _appSettingsService.GetWidgetsList();
        var widget = widgetList.FirstOrDefault(x => (WidgetType)Enum.Parse(typeof(WidgetType), x.Type) == widgetType);
        if (widget != null)
        {
            widget.Position = position;
            await _appSettingsService.SaveWidgetsList(widget);
        }
    }

    public async Task UpdateWidgetSize(WidgetType widgetType, WidgetSize size)
    {
        var widgetList = await _appSettingsService.GetWidgetsList();
        var widget = widgetList.FirstOrDefault(x => (WidgetType)Enum.Parse(typeof(WidgetType), x.Type) == widgetType);
        if (widget != null)
        {
            widget.Size = size;
            await _appSettingsService.SaveWidgetsList(widget);
        }
    }

    public async Task CloseWidget(WidgetType widgetType)
    {
        if (WidgetsDict.TryGetValue(widgetType, out var widgetWindow))
        {
            var widgetList = await _appSettingsService.GetWidgetsList();
            var widget = widgetList.FirstOrDefault(x => (WidgetType)Enum.Parse(typeof(WidgetType), x.Type) == widgetType);
            if (widget != null)
            {
                widget.IsEnabled = false;
                await _appSettingsService.SaveWidgetsList(widget);
            }

            widgetWindow.SetEditMode(false);
            widgetWindow.Close();
            WidgetsDict.Remove(widgetType);
        }
        if (!WidgetsDict.Keys.Any(TimerWidgets.Contains))
        {
            _timersService.StopUpdateTimeTimer();
        }
    }

    public void CloseAllWidgets()
    {
        foreach (var widgetWindow in WidgetsDict.Values)
        {
            widgetWindow.SetEditMode(false);
            widgetWindow.Close();
            WidgetsDict.Clear();
        }
    }

    public WidgetType GetWidgetType()
    {
        return currentWidgetType;
    }

    public BlankWindow GetWidgetWindow()
    {
        if (WidgetsDict.TryGetValue(currentWidgetType, out var widgetWindow))
        {
            return widgetWindow;
        }
        return null!;
    }

    public BlankWindow? GetWidgetWindow(WidgetType widgetType)
    {
        if (WidgetsDict.TryGetValue(widgetType, out var widgetWindow))
        {
            return widgetWindow;
        }
        return null;
    }

    public async Task SetThemeAsync()
    {
        foreach (var widgetWindow in WidgetsDict.Values.Where(x => x != null)!)
        {
            await _themeSelectorService.SetRequestedThemeAsync(widgetWindow);
        }
    }

    public List<DashboardWidgetItem> GetAllWidgets(Action<DashboardWidgetItem>? EnabledChangedCallback)
    {
        List<DashboardWidgetItem> dashboardItemList = new();

        foreach (WidgetType widgetType in Enum.GetValues(typeof(WidgetType)))
        {
            dashboardItemList.Add(new DashboardWidgetItem()
            {
                Type = widgetType,
                Label = _widgetResourceService.GetWidgetLabel(widgetType),
                IsEnabled = WidgetsDict.ContainsKey(widgetType),
                Icon = _widgetResourceService.GetWidgetIconSource(widgetType),
                EnabledChangedCallback = EnabledChangedCallback,
            });
        }

        return dashboardItemList;
    }
}
