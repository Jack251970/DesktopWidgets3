using DesktopWidgets3.Widget.Jack251970.Performance.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.Jack251970.Performance.Views;

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
