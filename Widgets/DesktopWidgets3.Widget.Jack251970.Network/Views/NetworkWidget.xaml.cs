using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.Jack251970.Network.Views;

public sealed partial class NetworkWidget : UserControl, IViewModel
{
    public NetworkViewModel ViewModel;

    BaseWidgetViewModel IViewModel.ViewModel => ViewModel;

    public NetworkWidget(ResourceDictionary? resourceDictionary, HardwareInfoService hardwareInfoService)
    {
        ViewModel = new NetworkViewModel(hardwareInfoService);
        if (resourceDictionary != null)
        {
            Resources.MergedDictionaries.Add(resourceDictionary);
        }
        InitializeComponent();
    }
}
