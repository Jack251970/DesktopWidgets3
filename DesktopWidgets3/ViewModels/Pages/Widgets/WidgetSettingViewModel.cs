using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;

namespace DesktopWidgets3.ViewModels.Pages.Widgets;

public partial class WidgetSettingViewModel : ObservableRecipient
{
    [ObservableProperty]
    public FrameworkElement _widgetFrameworkElement = null!;

    public WidgetSettingViewModel()
    {

    }
}
