using Microsoft.UI.Xaml.Controls;

using DesktopWidgets3.ViewModels.Pages;
using Microsoft.UI.Xaml;
using DesktopWidgets3.Models.Widget;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Controls.Primitives;
using static DesktopWidgets3.Services.WidgetDialogService;
using DesktopWidgets3.ViewModels.Pages.Widget.Settings;

namespace DesktopWidgets3.Views.Pages;

public sealed partial class DashboardPage : Page
{
    public DashboardViewModel ViewModel
    {
        get;
    }

    private WidgetType _widgetType;
    private int _indexTag = -1;

    private readonly IWidgetDialogService _dialogService;
    private readonly INavigationService _navigationService;

    public DashboardPage()
    {
        ViewModel = App.GetService<DashboardViewModel>();
        InitializeComponent();

        _dialogService = App.GetService<IWidgetDialogService>();
        _navigationService = App.GetService<INavigationService>();
    }

    private void AllWidgetsItemClick(object sender, RoutedEventArgs e)
    {
        var element = sender as FrameworkElement;
        if (element != null)
        {
            var widgetType = WidgetProperties.GetWidgetType(element);
            ViewModel.AllWidgetsItemClick(widgetType);
        }
    }

    private void WidgetItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        var element = sender as FrameworkElement;
        if (element != null)
        {
            _widgetType = WidgetProperties.GetWidgetType(element);
            _indexTag = WidgetProperties.GetIndexTag(element);

            element.ContextFlyout.ShowAt(element, new FlyoutShowOptions { Position = e.GetPosition(element) });
            e.Handled = true;
        }
    }

    private async void MenuFlyoutItemDeleteWidget_Click(object sender, RoutedEventArgs e)
    {
        if (_indexTag != -1)
        {
            if (await _dialogService.ShowDeleteWidgetDialog(App.MainWindow) == WidgetDialogResult.Left)
            {
                ViewModel.MenuFlyoutItemDeleteWidgetClick(_widgetType, _indexTag);
            }

            _indexTag = -1;
        }
    }

    private void WidgetItem_Click(object sender, RoutedEventArgs e)
    {
        var element = sender as FrameworkElement;
        if (element != null)
        {
            _widgetType = WidgetProperties.GetWidgetType(element);
            _indexTag = WidgetProperties.GetIndexTag(element);

            var parameter = new Dictionary<string, object>
            {
                { "WidgetType", _widgetType },
                { "IndexTag", _indexTag }
            };
            switch (_widgetType)
            {
                case WidgetType.Clock:
                    _navigationService.NavigateTo(typeof(ClockSettingsViewModel).FullName!, parameter);
                    break;
                case WidgetType.Performance:
                    _navigationService.NavigateTo(typeof(PerformanceSettingsViewModel).FullName!, parameter);
                    break;
                case WidgetType.Disk:
                    _navigationService.NavigateTo(typeof(DiskSettingsViewModel).FullName!, parameter);
                    break;
                case WidgetType.FolderView:
                    _navigationService.NavigateTo(typeof(FolderViewSettingsViewModel).FullName!, parameter);
                    break;
                case WidgetType.Network:
                    _navigationService.NavigateTo(typeof(NetworkSettingsViewModel).FullName!, parameter);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _indexTag = -1;
        }
    }
}
