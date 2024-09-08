using DesktopWidget3.Clock.ViewModel;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidget3.Clock.View;
public sealed partial class ClockWidget : UserControl
{
    public ClockViewModel ViewModel { get; }

    public ClockWidget()
    {
        InitializeComponent();
    }
}
