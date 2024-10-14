using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Core.Widgets.ViewModels.Pages;

public partial class WidgetViewModel : ObservableRecipient
{
    [ObservableProperty]
    public string _widgetIcoPath = string.Empty;

    [ObservableProperty]
    public string _widgetDisplayTitle = string.Empty;

    [ObservableProperty]
    public FrameworkElement _widgetFrameworkElement = new ProgressRing();

    [ObservableProperty]
    public MenuFlyout? _widgetMenuFlyout = null;

    public WidgetViewModel()
    {

    }
}
