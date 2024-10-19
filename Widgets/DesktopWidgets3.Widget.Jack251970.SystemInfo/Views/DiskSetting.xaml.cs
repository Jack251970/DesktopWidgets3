using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.Jack251970.SystemInfo.Views;

public sealed partial class DiskSetting : UserControl, ISettingViewModel
{
    public DiskSettingViewModel ViewModel;

    BaseWidgetSettingViewModel ISettingViewModel.ViewModel => ViewModel;

    public DiskSetting(ResourceDictionary? resourceDictionary)
    {
        ViewModel = new DiskSettingViewModel();
        if (resourceDictionary != null)
        {
            Resources.MergedDictionaries.Add(resourceDictionary);
        }
        InitializeComponent();
    }
}
