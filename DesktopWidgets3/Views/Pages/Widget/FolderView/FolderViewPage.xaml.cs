using Microsoft.UI.Xaml.Controls;

using DesktopWidgets3.ViewModels.Pages.Widget.FolderView;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Controls.Primitives;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Views.Windows;
using DesktopWidgets3.ViewModels.Pages;
using DesktopWidgets3.Models.Widget.FolderView;
using static DesktopWidgets3.Services.DialogService;

namespace DesktopWidgets3.Views.Pages.Widget.FolderView;

public sealed partial class FolderViewPage : Page
{
    public FolderViewViewModel ViewModel
    {
        get;
    }

    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;
    private readonly IWidgetManagerService _widgetManagerService;

    private readonly WidgetWindow _widgetWindow;

    public FolderViewPage()
    {
        ViewModel = App.GetService<FolderViewViewModel>();
        InitializeComponent();

        _dialogService = App.GetService<IDialogService>();
        _navigationService = App.GetService<INavigationService>();
        _widgetManagerService = App.GetService<IWidgetManagerService>();
        _widgetWindow = _widgetManagerService.GetCurrentWidgetWindow();
    }

    private async void NavigateBackButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.NavigateBackButtonClick();
    }

    private async void NavigateUpButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.NavigateUpButtonClick();
    }

    private async void NavigateRefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.NavigateRefreshButtonClick();
    }

    private async void FileList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement { DataContext: FolderViewFileItem item })
        {
            var filePath = item.FilePath;
            await ViewModel.FolderViewItemDoubleTapped(filePath);
        }
    }

    private void Toolbar_RightTapped(object sender, RightTappedRoutedEventArgs e)
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
