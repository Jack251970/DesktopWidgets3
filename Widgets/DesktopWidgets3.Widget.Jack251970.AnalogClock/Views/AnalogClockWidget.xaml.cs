using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.Jack251970.AnalogClock.Views;

public sealed partial class AnalogClockWidget : UserControl, IViewModel, IWidgetMenu
{
    public AnalogClockViewModel ViewModel;

    BaseWidgetViewModel IViewModel.ViewModel => ViewModel;

    public AnalogClockWidget(ResourceDictionary? resourceDictionary)
    {
        ViewModel = new AnalogClockViewModel();
        if (resourceDictionary != null)
        {
            Resources.MergedDictionaries.Add(resourceDictionary);
        }
        InitializeComponent();
    }

    public FrameworkElement GetWidgetMenuFrameworkElement()
    {
        return ContentArea;
    }
}
