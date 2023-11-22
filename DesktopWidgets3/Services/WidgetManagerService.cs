using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.ViewModels.WidgetsPages.Clock;
using DesktopWidgets3.Views.Windows;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Services;

public class WidgetManagerService : IWidgetManagerService
{
    private static WindowEx? ClockWindow
    {
        get; set;
    }

    private static WindowEx? CPUWindow
    {
        get; set;
    }

    private readonly IActivationService _activationService;

    private readonly IWidgetNavigationService _widgetNavigationService;

    public WidgetManagerService(IActivationService activationService, IWidgetNavigationService widgetNavigationService)
    {
        _activationService = activationService;
        _widgetNavigationService = widgetNavigationService;
    }

    public void ShowWidget(string widgetType)
    {
        if (widgetType == "Clock")
        {
            ClockWindow ??= new BlankWindow(widgetType);
            _ = _activationService.ActivateWidgetWindowAsync(ClockWindow);
            _widgetNavigationService.Frame = ClockWindow.Content as Frame;
            _widgetNavigationService.InitializeDefaultPage(typeof(ClockViewModel).FullName!);
            ClockWindow.Show();
        }
        else if (widgetType == "CPU")
        {
            CPUWindow ??= new BlankWindow(widgetType);
            _ = _activationService.ActivateWidgetWindowAsync(CPUWindow);
            CPUWindow.Show();
        }
    }

    public void CloseAllWidgets()
    {
        ClockWindow?.Close();
        CPUWindow?.Close();
    }
}
