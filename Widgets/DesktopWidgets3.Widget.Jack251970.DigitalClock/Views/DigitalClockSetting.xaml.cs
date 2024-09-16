using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.Jack251970.DigitalClock.Views;

public sealed partial class DigitalClockSetting : UserControl, ISettingViewModel
{
    public DigitalClockSettingViewModel ViewModel;

    BaseWidgetSettingViewModel ISettingViewModel.ViewModel => ViewModel;

    public DigitalClockSetting()
    {
        ViewModel = new DigitalClockSettingViewModel();
        InitializeComponent();
    }
}
