using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Widget.Contracts.Main;

public interface IWidgetSetting
{
    BaseWidgetSettings GetDefaultSetting();

    FrameworkElement CreateWidgetSettingFrameworkElement();
}
