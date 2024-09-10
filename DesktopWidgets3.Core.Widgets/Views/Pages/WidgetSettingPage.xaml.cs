using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Core.Widgets.Views.Pages;

public sealed partial class WidgetSettingPage : Page
{
    public WidgetSettingViewModel ViewModel { get; }

    public WidgetSettingPage()
    {
        ViewModel = DependencyExtensions.GetRequiredService<WidgetSettingViewModel>();
        InitializeComponent();
    }
}
