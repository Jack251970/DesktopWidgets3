using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.Jack251970.Network.Views;

public sealed partial class NetworkSetting : UserControl, ISettingViewModel
{
    public NetworkSettingViewModel ViewModel;

    BaseWidgetSettingViewModel ISettingViewModel.ViewModel => ViewModel;

    public NetworkSetting()
    {
        ViewModel = new NetworkSettingViewModel();
        InitializeComponent();
    }
}
