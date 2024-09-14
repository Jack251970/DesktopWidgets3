using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.Jack251970.Performance.Views;

public sealed partial class PerformanceSetting : UserControl, ISettingViewModel
{
    public PerformanceSettingViewModel ViewModel;

    BaseWidgetSettingViewModel ISettingViewModel.ViewModel => ViewModel;

    public PerformanceSetting(WidgetInitContext context)
    {
        ViewModel = new PerformanceSettingViewModel(context);
        InitializeComponent();
    }
}
