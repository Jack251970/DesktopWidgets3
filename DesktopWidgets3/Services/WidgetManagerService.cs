using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Models;
using DesktopWidgets3.Views.Windows;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.Graphics;

namespace DesktopWidgets3.Services;

public class WidgetManagerService : IWidgetManagerService
{
    private readonly Dictionary<WidgetType, BlankWindow> WidgetsDict = new();

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

    public WidgetManagerService(IActivationService activationService, IAppSettingsService appSettingsService, IThemeSelectorService themeSelectorService, ITimersService timersService, IWidgetResourceService widgetResourceService)
    {
        _activationService = activationService;
        _appSettingsService = appSettingsService;
        _themeSelectorService = themeSelectorService;
        _timersService = timersService;
        _widgetResourceService = widgetResourceService;
    }

    public void InitializeWidgets()
    {
#if DEBUG
        return;
#else
        var widgetList = _appSettingsService.GetWidgetsList();
        var enableTimer = false;
        foreach (var widget in widgetList)
        {
            if (widget.IsEnabled)
            {
                var widgetType = (WidgetType)Enum.Parse(typeof(WidgetType), widget.Type);
                ShowWidget(widgetType);
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
#endif
    }

    public void ShowWidget(WidgetType widgetType)
    {
        if (!WidgetsDict.TryGetValue(widgetType, out var widgetWindow))
        {
            var widgetList = _appSettingsService.GetWidgetsList();
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
                _appSettingsService.SaveWidgetsList(widget);
            }
            else
            {
                widget.IsEnabled = true;
                _appSettingsService.SaveWidgetsList(widget);
            }

            var blankWindow = new BlankWindow(widgetType);
            WidgetsDict.Add(widgetType, blankWindow);
            _ = _activationService.ActivateWidgetWindowAsync(blankWindow);
            var frame = blankWindow.Content as Frame;
            blankWindow.InitializePage(frame, widgetType, widget.Position, widget.Size);
            blankWindow.Show();
        }
        if (TimerWidgets.Contains(widgetType))
        {
            _timersService.StartUpdateTimeTimer();
        }
    }

    public async void UpdateWidgetPosition(WidgetType widgetType, PointInt32 position)
    {
        var widgetList = _appSettingsService.GetWidgetsList();
        var widget = widgetList.FirstOrDefault(x => (WidgetType)Enum.Parse(typeof(WidgetType), x.Type) == widgetType);
        if (widget != null)
        {
            widget.Position = position;
            await _appSettingsService.SaveWidgetsList(widget);
        }
    }

    public async void UpdateWidgetSize(WidgetType widgetType, Size size)
    {
        var widgetList = _appSettingsService.GetWidgetsList();
        var widget = widgetList.FirstOrDefault(x => (WidgetType)Enum.Parse(typeof(WidgetType), x.Type) == widgetType);
        if (widget != null)
        {
            widget.Size = size;
            await _appSettingsService.SaveWidgetsList(widget);
        }
    }

    public void SetEditMode(bool isEditMode)
    {
        foreach (var widgetWindow in WidgetsDict.Values)
        {
            widgetWindow.SetEditMode(isEditMode);
        }
    }

    public void CloseWidget(WidgetType widgetType)
    {
        if (WidgetsDict.TryGetValue(widgetType, out var widgetWindow))
        {
            var widgetList = _appSettingsService.GetWidgetsList();
            var widget = widgetList.FirstOrDefault(x => (WidgetType)Enum.Parse(typeof(WidgetType), x.Type) == widgetType);
            if (widget != null)
            {
                widget.IsEnabled = false;
                _appSettingsService.SaveWidgetsList(widget);
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

    public IEnumerable<BlankWindow> GetWidgets()
    {
        return WidgetsDict.Values.Where(x => x != null)!;
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

        foreach (WidgetType moduleType in Enum.GetValues(typeof(WidgetType)))
        {
            dashboardItemList.Add(new DashboardWidgetItem()
            {
                Type = moduleType,
                Label = moduleType.ToString(),
                IsEnabled = WidgetsDict.ContainsKey(moduleType),
                Icon = null,
                EnabledChangedCallback = EnabledChangedCallback,
            });
        }

        return dashboardItemList;
    }
}
