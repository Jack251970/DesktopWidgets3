using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.Jack251970.Disk.Views;

public sealed partial class DiskSetting : UserControl, ISettingViewModel
{
    public DiskSettingViewModel ViewModel;

    BaseWidgetSettingViewModel ISettingViewModel.ViewModel => ViewModel;

    public DiskSetting()
    {
        ViewModel = new DiskSettingViewModel();
        InitializeComponent();
    }
}
