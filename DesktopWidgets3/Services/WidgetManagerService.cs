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
        if (!WidgetsDict.TryGetValue(widgetType, out var widgetWindow))
        {
            var blankWindow = new BlankWindow(widgetType);
            WidgetsDict.Add(widgetType, blankWindow);
            _ = _activationService.ActivateWidgetWindowAsync(blankWindow);
            var frame = blankWindow.Content as Frame;
            switch (widgetType)
            {
                case WidgetType.Clock:
                    blankWindow.InitializePage(frame, typeof(ClockViewModel).FullName!);
                    break;
                case WidgetType.CPU:
                    blankWindow.InitializePage(frame, typeof(ClockViewModel).FullName!);
                    break;
                case WidgetType.Disk:
                    blankWindow.InitializePage(frame, typeof(ClockViewModel).FullName!);
                    break;
                case WidgetType.Network:
                    blankWindow.InitializePage(frame, typeof(ClockViewModel).FullName!);
                    break;
                case WidgetType.Folder:
                    blankWindow.InitializePage(frame, typeof(ClockViewModel).FullName!);
                    break;
            }
            blankWindow.Show();
        }
        else
        {
            widgetWindow.Show();
        }
    }

    public void CloseWidget(WidgetType widgetType)
    {
        if (WidgetsDict.TryGetValue(widgetType, out var widgetWindow))
        {
            widgetWindow.Close();
            WidgetsDict.Remove(widgetType);
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
