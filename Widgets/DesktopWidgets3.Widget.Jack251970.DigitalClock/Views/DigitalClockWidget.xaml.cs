using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.Jack251970.DigitalClock.Views;

public sealed partial class DigitalClockWidget : UserControl, IViewModel, IWidgetMenu
{
    public DigitalClockViewModel ViewModel;

    BaseWidgetViewModel IViewModel.ViewModel => ViewModel;

    public DigitalClockWidget()
    {
        ViewModel = new DigitalClockViewModel();
        InitializeComponent();
    }

    public FrameworkElement GetWidgetMenuFrameworkElement()
    {
        return ContentArea;
    }
}
