using DesktopWidgets3.Widget.Jack251970.Disk.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.Jack251970.Disk.Views;

public sealed partial class DigitalClockSetting : UserControl, ISettingViewModel
{
    public DigitalClockSettingViewModel ViewModel;

    BaseWidgetSettingViewModel ISettingViewModel.ViewModel => ViewModel;

    public DigitalClockSetting(WidgetInitContext context)
    {
        ViewModel = new DigitalClockSettingViewModel(context);
        InitializeComponent();
    }
}
