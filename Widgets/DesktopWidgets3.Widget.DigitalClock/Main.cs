using DesktopWidgets3.Widget.DigitalClock.View;
using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Widget.DigitalClock;

public class Main : IWidget
{
    public FrameworkElement CreateWidgetPage()
    {
        return new ClockWidget();
    }

    public void InitWidgetClass(WidgetInitContext context)
    {

    }

    public void InitWidgetInstance(WidgetInitContext context)
    {

    }
}
