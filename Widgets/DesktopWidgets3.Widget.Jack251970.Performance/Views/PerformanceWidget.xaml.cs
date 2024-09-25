using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.Jack251970.Performance.Views;

public sealed partial class PerformanceWidget : UserControl, IViewModel
{
    public PerformanceViewModel ViewModel;

    BaseWidgetViewModel IViewModel.ViewModel => ViewModel;

    public PerformanceWidget(ResourceDictionary? resourceDictionary, HardwareInfoService hardwareInfoService)
    {
        ViewModel = new PerformanceViewModel(hardwareInfoService);
        if (resourceDictionary != null)
        {
            Resources.MergedDictionaries.Add(resourceDictionary);
        }
        InitializeComponent();
    }
}
