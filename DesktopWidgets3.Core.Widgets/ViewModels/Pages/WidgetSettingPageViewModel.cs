using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Core.Widgets.ViewModels.Pages;

public partial class WidgetSettingPageViewModel : ObservableRecipient
{
    [ObservableProperty]
    public FrameworkElement _widgetFrameworkElement = null!;

    public WidgetSettingPageViewModel()
    {

    }
}
