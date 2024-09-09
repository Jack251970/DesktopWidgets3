using DesktopWidgets3.Widget.DigitalClock.ViewModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.DigitalClock.View;

public sealed partial class ClockWidget : UserControl, IWidgetMenu
{
    public ClockViewModel ViewModel = new();

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
