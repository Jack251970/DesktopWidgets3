using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.Jack251970.AnalogClock.Views;

public sealed partial class AnalogClockSetting : UserControl, ISettingViewModel
{
    public AnalogClockSettingViewModel ViewModel;

    BaseWidgetSettingViewModel ISettingViewModel.ViewModel => ViewModel;

    public AnalogClockSetting(ResourceDictionary? resourceDictionary)
    {
        ViewModel = new AnalogClockSettingViewModel();
        if (resourceDictionary != null)
        {
            Resources.MergedDictionaries.Add(resourceDictionary);
        }
        InitializeComponent();
    }
}
