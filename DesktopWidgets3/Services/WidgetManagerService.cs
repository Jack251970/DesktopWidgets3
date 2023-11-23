using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.ViewModels.WidgetsPages.Clock;
using DesktopWidgets3.Views.Windows;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Services;

public class WidgetManagerService : IWidgetManagerService
{
    private readonly Dictionary<string, WindowEx> WidgetsDict = new() {};

    private readonly IActivationService _activationService;
    private readonly IThemeSelectorService _themeSelectorService;

    public WidgetManagerService(IActivationService activationService, IThemeSelectorService themeSelectorService)
    {
        _activationService = activationService;
        _themeSelectorService = themeSelectorService;
    }

    public void ShowWidget(string widgetType)
    {
        if (!WidgetsDict.TryGetValue(widgetType, out var value))
        {
            var widgetWindow = new BlankWindow(widgetType);
            WidgetsDict.Add(widgetType, widgetWindow);
            _ = _activationService.ActivateWidgetWindowAsync(widgetWindow);
            var frame = widgetWindow.Content as Frame;
            switch (widgetType)
            {
                case "Clock":
                    widgetWindow.InitializePage(frame, typeof(ClockViewModel).FullName!);
                    break;
                case "CPU":
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

    public void CloseWidget(string widgetType)
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

    public IEnumerable<WindowEx> GetWidgets()
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
}
