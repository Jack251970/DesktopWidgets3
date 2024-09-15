using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.Jack251970.AnalogClock.Views;

public sealed partial class AnalogClockWidget : UserControl, IViewModel, IWidgetMenu
{
    public AnalogClockViewModel ViewModel;

    BaseWidgetViewModel IViewModel.ViewModel => ViewModel;

    public AnalogClockWidget(WidgetInitContext context)
    {
        ViewModel = new AnalogClockViewModel(context);
        InitializeComponent();
    }

    public FrameworkElement GetWidgetMenuFrameworkElement()
    {
        return ContentArea;
    }
}
