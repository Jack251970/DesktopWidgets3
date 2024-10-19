using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.Jack251970.Clock.Views;

public sealed partial class DigitalClockSetting : UserControl, ISettingViewModel
{
    public DigitalClockSettingViewModel ViewModel;

    BaseWidgetSettingViewModel ISettingViewModel.ViewModel => ViewModel;

    public DigitalClockSetting(ResourceDictionary? resourceDictionary)
    {
        ViewModel = new DigitalClockSettingViewModel();
        if (resourceDictionary != null)
        {
            Resources.MergedDictionaries.Add(resourceDictionary);
        }
        InitializeComponent();
    }
}
