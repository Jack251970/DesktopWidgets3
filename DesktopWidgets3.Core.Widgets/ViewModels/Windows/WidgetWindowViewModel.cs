using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace DesktopWidgets3.Core.Widgets.ViewModels.Windows;

public partial class WidgetWindowViewModel : ObservableRecipient
{
    [ObservableProperty]
    public Brush? _widgetIconFill = null;

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
