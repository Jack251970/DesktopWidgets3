using DesktopWidget3.Clock.ViewModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidget3.Clock.View;

public sealed partial class ClockPage : Page
{
    public ClockViewModel ViewModel { get; }

    public ClockPage()
    {
        InitializeComponent();
    }

    public FrameworkElement GetWidgetMenuFrameworkElement()
    {
        return ContentArea;
    }
}
