using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Views.Pages.Widgets;

public sealed partial class WidgetSettingPage : Page
{
    public WidgetSettingViewModel ViewModel { get; }

    public WidgetSettingPage()
    {
        ViewModel = App.GetService<WidgetSettingViewModel>();
        InitializeComponent();
    }
}
