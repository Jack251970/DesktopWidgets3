using Microsoft.UI.Xaml.Controls;

using DesktopWidgets3.ViewModels.Pages;
using Microsoft.UI.Xaml;
using DesktopWidgets3.Models.Widget;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace DesktopWidgets3.Views.Pages;

public sealed partial class DashboardPage : Page
{
    public DashboardViewModel ViewModel
    {
        get;
    }

    private WidgetType _widgetType;
    private int _indexTag = -1;

    public DashboardPage()
    {
        ViewModel = App.GetService<DashboardViewModel>();
        InitializeComponent();
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

    private void MenuFlyoutItemDeleteWidget_Click(object sender, RoutedEventArgs e)
    {
        if (_indexTag != -1)
        {
            ViewModel.MenuFlyoutItemDeleteWidgetClick(_widgetType, _indexTag);

            _indexTag = -1;
        }
    }
}
