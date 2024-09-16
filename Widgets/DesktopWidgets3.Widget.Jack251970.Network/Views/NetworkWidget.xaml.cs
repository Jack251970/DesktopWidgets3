using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.Jack251970.Network.Views;

public sealed partial class NetworkWidget : UserControl, IViewModel, IWidgetMenu
{
    public NetworkViewModel ViewModel;

    BaseWidgetViewModel IViewModel.ViewModel => ViewModel;

    public NetworkWidget(HardwareInfoService hardwareInfoService)
    {
        ViewModel = new NetworkViewModel(hardwareInfoService);
        InitializeComponent();
    }

    public FrameworkElement GetWidgetMenuFrameworkElement()
    {
        return ContentArea;
    }
}
