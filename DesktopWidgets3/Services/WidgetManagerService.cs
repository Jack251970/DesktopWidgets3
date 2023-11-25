using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Models;
using DesktopWidgets3.Views.Windows;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Services;

public class WidgetManagerService : IWidgetManagerService
{
    private readonly Dictionary<WidgetType, BlankWindow> WidgetsDict = new();

    private readonly List<WidgetType> TimerWidgets = new()
    {
        WidgetType.Clock,
    };

    private readonly IActivationService _activationService;
    private readonly IAppSettingsService _appSettingsService;
    private readonly IThemeSelectorService _themeSelectorService;
    private readonly ITimersService _timersService;

    public WidgetManagerService(IActivationService activationService, IAppSettingsService appSettingsService, IThemeSelectorService themeSelectorService, ITimersService timersService)
    {
        _activationService = activationService;
        _appSettingsService = appSettingsService;
        _themeSelectorService = themeSelectorService;
        _timersService = timersService;
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
                ShowWidget(WidgetItemUtils.ConvertToBaseWidgetItem(widget).Type);
                if (timerWidgets.Contains(widget.Type))
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
            var blankWindow = new BlankWindow(widgetType);
            WidgetsDict.Add(widgetType, blankWindow);
            _ = _activationService.ActivateWidgetWindowAsync(blankWindow);
            var frame = blankWindow.Content as Frame;
            blankWindow.InitializePage(frame, widgetType);
            blankWindow.Show();
        }
        else
        {
            widgetWindow.Show();
        }
        if (TimerWidgets.Contains(widgetType))
        {
            _timersService.StartUpdateTimeTimer();
        }
    }

    public void CloseWidget(WidgetType widgetType)
    {
        if (WidgetsDict.TryGetValue(widgetType, out var widgetWindow))
        {
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
            widgetWindow.Close();
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
