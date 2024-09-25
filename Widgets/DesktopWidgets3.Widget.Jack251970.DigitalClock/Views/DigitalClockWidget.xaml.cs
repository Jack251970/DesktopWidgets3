using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.Jack251970.DigitalClock.Views;

public sealed partial class DigitalClockWidget : UserControl, IViewModel
{
    public DigitalClockViewModel ViewModel;

    BaseWidgetViewModel IViewModel.ViewModel => ViewModel;

    public DigitalClockWidget(ResourceDictionary? resourceDictionary)
    {
        ViewModel = new DigitalClockViewModel();
        if (resourceDictionary != null)
        {
            Resources.MergedDictionaries.Add(resourceDictionary);
        }
        InitializeComponent();
    }
}
