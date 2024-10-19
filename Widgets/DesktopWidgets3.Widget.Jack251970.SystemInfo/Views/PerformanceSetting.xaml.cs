using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.Jack251970.SystemInfo.Views;

public sealed partial class PerformanceSetting : UserControl, ISettingViewModel
{
    public PerformanceSettingViewModel ViewModel;

    BaseWidgetSettingViewModel ISettingViewModel.ViewModel => ViewModel;

    public PerformanceSetting(ResourceDictionary? resourceDictionary)
    {
        ViewModel = new PerformanceSettingViewModel();
        if (resourceDictionary != null)
        {
            Resources.MergedDictionaries.Add(resourceDictionary);
        }
        InitializeComponent();
    }
}
