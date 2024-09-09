using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.Contracts.Main;

public interface IWidgetSetting
{
    BaseWidgetSettings GetDefaultSetting();

    Page CreateSettingPage();
}
