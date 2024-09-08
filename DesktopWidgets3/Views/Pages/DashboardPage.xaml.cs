using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace DesktopWidgets3.Views.Pages;

public sealed partial class DashboardPage : Page
{
    public DashboardViewModel ViewModel { get; }

    private string _widgetId;
    private int _indexTag = -1;

    private readonly INavigationService _navigationService;

    public DashboardPage()
    {
        ViewModel = App.GetService<DashboardViewModel>();
        InitializeComponent();

        _navigationService = App.GetService<INavigationService>();
    }

    private void AllWidgetsItemClick(object sender, RoutedEventArgs e)
    {
        var element = sender as FrameworkElement;
        if (element != null)
        {
            var widgetId = WidgetProperties.GetId(element);
            ViewModel.AllWidgetsItemClick(widgetId);
        }
    }

    private void WidgetItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        var element = sender as FrameworkElement;
        if (element != null)
        {
            _widgetId = WidgetProperties.GetId(element);
            _indexTag = WidgetProperties.GetIndexTag(element);

            element.ContextFlyout.ShowAt(element, new FlyoutShowOptions { Position = e.GetPosition(element) });
            e.Handled = true;
        }
    }

    private async void MenuFlyoutItemDeleteWidget_Click(object sender, RoutedEventArgs e)
    {
        if (_indexTag != -1)
        {
            if (await App.MainWindow.ShowDeleteWidgetDialog() == WidgetDialogResult.Left)
            {
                ViewModel.MenuFlyoutItemDeleteWidgetClick(_widgetId, _indexTag);
            }

            _indexTag = -1;
        }
    }

    private void WidgetItem_Click(object sender, RoutedEventArgs e)
    {
        var element = sender as FrameworkElement;
        if (element != null)
        {
            _widgetId = WidgetProperties.GetId(element);
            _indexTag = WidgetProperties.GetIndexTag(element);

            var parameter = new Dictionary<string, object>
            {
                { "Id", _widgetId },
                { "IndexTag", _indexTag }
            };
            // TODO: _navigationService.NavigateTo("WidgetSettings", parameter);

            _indexTag = -1;
        }
    }
}
