using DesktopWidget3.DigitalClock.View;
using Microsoft.UI.Xaml;

namespace DesktopWidget3.DigitalClock;

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
