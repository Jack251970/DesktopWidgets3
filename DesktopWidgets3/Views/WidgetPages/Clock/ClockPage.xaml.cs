using Microsoft.UI.Xaml.Controls;

using DesktopWidgets3.ViewModels.WidgetsPages.Clock;

namespace DesktopWidgets3.Views.WidgetPages.Clock;

public sealed partial class ClockPage : Page
{
    public ClockViewModel ViewModel
    {
        get;
    }

    public ClockPage()
    {
        ViewModel = App.GetService<ClockViewModel>();
        InitializeComponent();
    }
}
