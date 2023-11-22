using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Views.Windows;

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

    public WidgetManagerService()
    {

    }

    public void ShowWidget(string widgetType)
    {
        if (widgetType == "Clock")
        {
            ClockWindow ??= new BlankWindow(widgetType);
            ClockWindow.Show();
            ClockWindow.Activate();
        }
        else if (widgetType == "CPU")
        {
            CPUWindow ??= new BlankWindow(widgetType);
            CPUWindow.Show();
            CPUWindow.Activate();
        }
    }

    public void CloseAllWidgets()
    {
        ClockWindow?.Close();
        CPUWindow?.Close();
    }
}
