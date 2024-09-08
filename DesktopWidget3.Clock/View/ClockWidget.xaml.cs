using DesktopWidget3.Clock.ViewModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidget3.Clock.View;

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
