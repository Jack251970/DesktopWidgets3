using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.Jack251970.Performance.Views;

public sealed partial class PerformanceWidget : UserControl, IViewModel, IWidgetMenu
{
    public PerformanceViewModel ViewModel;

    BaseWidgetViewModel IViewModel.ViewModel => ViewModel;

    public PerformanceWidget(HardwareInfoService hardwareInfoService)
    {
        ViewModel = new PerformanceViewModel(hardwareInfoService);
        InitializeComponent();
    }

    public FrameworkElement GetWidgetMenuFrameworkElement()
    {
        return ContentArea;
    }
}
