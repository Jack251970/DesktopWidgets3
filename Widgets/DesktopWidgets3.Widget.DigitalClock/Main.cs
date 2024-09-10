using DesktopWidgets3.Widget.DigitalClock.Setting;
using DesktopWidgets3.Widget.DigitalClock.Views;
using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Widget.DigitalClock;

public class Main : IWidget, IWidgetSetting
{
    #region IWidget

    private WidgetInitContext Context = null!;

    public FrameworkElement CreateWidgetFrameworkElement()
    {
        return new ClockWidget();
    }

    public void InitWidget(WidgetInitContext context)
    {
        Context = context;
    }

    public void InitWidgetInstance(bool firstWidget)
    {

    }

    #endregion

    #region IWidgetSetting

    public BaseWidgetSettings GetDefaultSetting()
    {
        return new DigitalClockSettings();
    }

    public FrameworkElement CreateWidgetSettingFrameworkElement()
    {
        return new DigitalClockSetting(Context);
    }

    #endregion
}
