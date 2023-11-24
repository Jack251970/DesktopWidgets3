using Microsoft.UI.Xaml.Controls;

using DesktopWidgets3.ViewModels.Pages;

namespace DesktopWidgets3.Views.Pages;

public sealed partial class WidgetSettingPage : Page
{
    public WidgetSettingViewModel ViewModel
    {
        get;
    }

    public WidgetSettingPage()
    {
        ViewModel = App.GetService<WidgetSettingViewModel>();
        InitializeComponent();
    }
}
