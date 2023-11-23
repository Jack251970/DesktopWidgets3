using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Models;
using DesktopWidgets3.ViewModels.WidgetsPages.Clock;
using DesktopWidgets3.Views.Windows;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Services;

public class WidgetManagerService : IWidgetManagerService
{
    private readonly Dictionary<WidgetType, BlankWindow> WidgetsDict = new() {};

    private readonly IActivationService _activationService;
    private readonly IThemeSelectorService _themeSelectorService;

    public WidgetManagerService(IActivationService activationService, IThemeSelectorService themeSelectorService)
    {
        _activationService = activationService;
        _themeSelectorService = themeSelectorService;
    }

    public void ShowWidget(WidgetType widgetType)
    {
        if (!WidgetsDict.TryGetValue(widgetType, out var value))
        {
            var widgetWindow = new BlankWindow(widgetType);
            WidgetsDict.Add(widgetType, widgetWindow);
            _ = _activationService.ActivateWidgetWindowAsync(widgetWindow);
            var frame = widgetWindow.Content as Frame;
            switch (widgetType)
            {
                case WidgetType.Clock:
                    widgetWindow.InitializePage(frame, typeof(ClockViewModel).FullName!);
                    break;
                case WidgetType.CPU:
                    widgetWindow.InitializePage(frame, typeof(ClockViewModel).FullName!);
                    break;
            }
            widgetWindow.Show();
        }
        else
        {
            value.Show();
        }
    }

    public void CloseWidget(WidgetType widgetType)
    {
        if (WidgetsDict.TryGetValue(widgetType, out var value))
        {
            value.Close();
            WidgetsDict.Remove(widgetType);
        }
    }

    public void CloseAllWidgets()
    {
        foreach (var window in WidgetsDict.Values)
        {
            window.Close();
        }
    }

    public IEnumerable<BlankWindow> GetWidgets()
    {
        return WidgetsDict.Values.Where(x => x != null)!;
    }

    public async Task SetThemeAsync()
    {
        foreach (var window in WidgetsDict.Values.Where(x => x != null)!)
        {
            await _themeSelectorService.SetRequestedThemeAsync(window);
        }
    }

    public List<WidgetItem> GetAllWidgets(Action<WidgetItem>? EnabledChangedCallback)
    {
        List<WidgetItem> dashboardItemList = new();

        foreach (WidgetType moduleType in Enum.GetValues(typeof(WidgetType)))
        {
            dashboardItemList.Add(new WidgetItem()
            {
                Tag = moduleType,
                Label = moduleType.ToString(),
                IsEnabled = WidgetsDict.ContainsKey(moduleType),
                Icon = null,
                EnabledChangedCallback = EnabledChangedCallback,
            });
        }

        return dashboardItemList;
    }
}
