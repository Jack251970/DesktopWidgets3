using DesktopWidgets3.Widget.DigitalClock.Setting;
using DesktopWidgets3.Widget.DigitalClock.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.DigitalClock;

public class Main : IWidget, IWidgetSetting
{
    #region IWidget

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

    #endregion

    #region IWidgetSetting

    public BaseWidgetSettings GetDefaultSetting()
    {
        return new DigitalClockSetting();
    }

    public FrameworkElement CreateWidgetSettingFrameworkElement()
    {
        return new UserControl();
    }

    #endregion
}
