using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Core.Widgets.Views.Pages;

public sealed partial class WidgetSettingPage : Page
{
    public WidgetSettingPageViewModel ViewModel { get; }

    public WidgetSettingPage()
    {
        ViewModel = DependencyExtensions.GetRequiredService<WidgetSettingPageViewModel>();
        InitializeComponent();
    }
}
