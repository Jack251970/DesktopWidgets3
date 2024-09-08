using DesktopWidget3.Clock.View;
using Microsoft.UI.Xaml;

namespace DesktopWidget3.Clock;

public class Main : IWidget
{
    public FrameworkElement CreateWidgetFrameworkElement()
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
