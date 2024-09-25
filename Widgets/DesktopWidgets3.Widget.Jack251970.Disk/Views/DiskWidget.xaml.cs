using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Widget.Jack251970.Disk.Views;

public sealed partial class DiskWidget : UserControl, IViewModel
{
    public DiskViewModel ViewModel;

    BaseWidgetViewModel IViewModel.ViewModel => ViewModel;

    public DiskWidget(ResourceDictionary? resourceDictionary, HardwareInfoService hardwareInfoService)
    {
        ViewModel = new DiskViewModel(hardwareInfoService);
        if (resourceDictionary != null)
        {
            Resources.MergedDictionaries.Add(resourceDictionary);
        }
        InitializeComponent();
    }
}
