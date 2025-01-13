using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Core.Widgets.ViewModels.Windows;

public partial class WidgetWindowViewModel : ObservableRecipient
{
    [ObservableProperty]
    public string _widgetIconPath = string.Empty;

    [ObservableProperty]
    public string _widgetDisplayTitle = string.Empty;

    [ObservableProperty]
    public FrameworkElement _widgetFrameworkElement = new ProgressRing();

    [ObservableProperty]
    public MenuFlyout? _widgetMenuFlyout = null;

    public WidgetWindowViewModel()
    {

    }
}
