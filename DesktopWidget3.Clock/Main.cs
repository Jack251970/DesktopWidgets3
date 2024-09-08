using DesktopWidget3.Clock.View;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidget3.Clock;

public class Main : IWidget
{
    public Page CreateWidgetPage()
    {
        return new ClockPage();
    }

    public void InitWidgetClass(WidgetInitContext context)
    {

    }

    public void InitWidgetInstance(WidgetInitContext context)
    {

    }
}
