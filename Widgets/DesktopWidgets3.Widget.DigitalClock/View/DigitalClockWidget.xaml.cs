using DesktopWidgets3.Widget.DigitalClock.ViewModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.DigitalClock.View;

public sealed partial class ClockWidget : UserControl, IViewModel, IWidgetMenu
{
    public ClockViewModel ViewModel = new();

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
