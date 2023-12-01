using Microsoft.UI.Xaml.Controls;

using DesktopWidgets3.ViewModels.Pages.Widget.Clock;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Views.Windows;

namespace DesktopWidgets3.Views.Pages.Widget.Clock;

public sealed partial class ClockPage : Page
{
    public ClockViewModel ViewModel
    {
        get;
    }

    public BlankWindow WidgetWindow
    {
        get;
    }

    private readonly IWidgetManagerService _widgetManagerService;

    public ClockPage()
    {
        ViewModel = App.GetService<ClockViewModel>();
        InitializeComponent();

        _widgetManagerService = App.GetService<IWidgetManagerService>();
        WidgetWindow = _widgetManagerService.GetCurrentWidgetWindow();
    }

    private void MenuFlyoutItemEnterEidtMode_Click(object sender, RoutedEventArgs e)
    {
        _widgetManagerService.EnterEditMode();
    }

    private void FolderViewFlyoutDisableWidget_Click(object sender, RoutedEventArgs e)
    {
        _widgetManagerService.DisableWidget(WidgetWindow);
    }

    private void ContentArea_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        var element = sender as FrameworkElement;
        if (element != null)
        {
            element.ContextFlyout.ShowAt(element, new FlyoutShowOptions { Position = e.GetPosition(element) });
            e.Handled = true;
        }
    }
}
