using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.Jack251970.SystemInfo.Views;

public sealed partial class NetworkSetting : UserControl, ISettingViewModel
{
    public NetworkSettingViewModel ViewModel;

    BaseWidgetSettingViewModel ISettingViewModel.ViewModel => ViewModel;

    public NetworkSetting(ResourceDictionary? resourceDictionary)
    {
        ViewModel = new NetworkSettingViewModel();
        if (resourceDictionary != null)
        {
            Resources.MergedDictionaries.Add(resourceDictionary);
        }
        InitializeComponent();
    }
}
