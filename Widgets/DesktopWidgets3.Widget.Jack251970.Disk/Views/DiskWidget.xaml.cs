using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.Jack251970.Disk.Views;

public sealed partial class DiskWidget : UserControl, IViewModel, IWidgetMenu
{
    public DiskViewModel ViewModel;

    BaseWidgetViewModel IViewModel.ViewModel => ViewModel;

    public DiskWidget(WidgetInitContext context, HardwareInfoService hardwareInfoService)
    {
        ViewModel = new DiskViewModel(context, hardwareInfoService);
        InitializeComponent();
    }

    public FrameworkElement GetWidgetMenuFrameworkElement()
    {
        return ContentArea;
    }
}
