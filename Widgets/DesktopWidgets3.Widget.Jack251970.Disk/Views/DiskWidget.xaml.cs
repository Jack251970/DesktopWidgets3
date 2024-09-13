using DesktopWidgets3.Widget.Jack251970.Disk.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.Jack251970.Disk.Views;

public sealed partial class ClockWidget : UserControl, IViewModel, IWidgetMenu
{
    public DigitalClockViewModel ViewModel = new();

    BaseWidgetViewModel IViewModel.ViewModel => ViewModel;

    public ClockWidget()
    {
        DataContext = ViewModel;
        InitializeComponent();
    }

    public FrameworkElement GetWidgetMenuFrameworkElement()
    {
        return ContentArea;
    }
}
