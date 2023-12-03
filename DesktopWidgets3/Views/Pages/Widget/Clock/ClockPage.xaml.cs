using Microsoft.UI.Xaml.Controls;

using DesktopWidgets3.ViewModels.Pages.Widget.Clock;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Views.Windows;
using DesktopWidgets3.ViewModels.Pages;
using static DesktopWidgets3.Services.DialogService;

namespace DesktopWidgets3.Views.Pages.Widget.Clock;

public sealed partial class ClockPage : Page
{
    public ClockViewModel ViewModel
    {
        get;
    }

    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;
    private readonly IWidgetManagerService _widgetManagerService;

    private readonly WidgetWindow _widgetWindow;

    public ClockPage()
    {
        ViewModel = App.GetService<ClockViewModel>();
        InitializeComponent();

        _dialogService = App.GetService<IDialogService>();
        _navigationService = App.GetService<INavigationService>();
        _widgetManagerService = App.GetService<IWidgetManagerService>();
        _widgetWindow = _widgetManagerService.GetCurrentWidgetWindow();
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

    private void MenuFlyoutItemDisableWidget_Click(object sender, RoutedEventArgs e)
    {
        var widgetType = _widgetWindow.WidgetType;
        var indexTag = _widgetWindow.IndexTag;
        _widgetManagerService.DisableWidget(widgetType, indexTag);
        var dashboardPageKey = typeof(DashboardViewModel).FullName!;
        var parameter = new Dictionary<string, object>
        {
            { "UpdateEvent", DashboardViewModel.UpdateEvent.Disable },
            { "WidgetType", _widgetWindow.WidgetType },
            { "IndexTag", _widgetWindow.IndexTag }
        };
        if (_navigationService.GetCurrentPageKey() == dashboardPageKey)
        {
            _navigationService.NavigateTo(dashboardPageKey, parameter);
        }
        else
        {
            _navigationService.SetNextParameter(dashboardPageKey, parameter);
        }
    }

    private async void MenuFlyoutItemDeleteWidget_Click(object sender, RoutedEventArgs e)
    {
        if (await _dialogService.ShowDeleteWidgetDialog(_widgetWindow) == DialogResult.Left)
        {
            var widgetType = _widgetWindow.WidgetType;
            var indexTag = _widgetWindow.IndexTag;
            await _widgetManagerService.DeleteWidget(widgetType, indexTag);
            var dashboardPageKey = typeof(DashboardViewModel).FullName!;
            var parameter = new Dictionary<string, object>
            {
                { "UpdateEvent", DashboardViewModel.UpdateEvent.Delete },
                { "WidgetType", _widgetWindow.WidgetType },
                { "IndexTag", _widgetWindow.IndexTag }
            };
            if (_navigationService.GetCurrentPageKey() == dashboardPageKey)
            {
                _navigationService.NavigateTo(dashboardPageKey, parameter);
            }
            else
            {
                _navigationService.SetNextParameter(dashboardPageKey, parameter);
            }
        }
    }

    private void MenuFlyoutItemEnterEidtMode_Click(object sender, RoutedEventArgs e)
    {
        _widgetManagerService.EnterEditMode();
    }
}
