using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace DesktopWidgets3.Views.Pages;

public sealed partial class DashboardPage : Page
{
    public DashboardViewModel ViewModel { get; }

    private readonly IWidgetManagerService _widgetManagerService = DependencyExtensions.GetRequiredService<IWidgetManagerService>();

    private readonly MenuFlyout RightClickMenu;

    private string _widgetId = string.Empty;
    private int _indexTag = -1;

    public DashboardPage()
    {
        ViewModel = DependencyExtensions.GetRequiredService<DashboardViewModel>();
        RightClickMenu = GetRightClickMenu();
        InitializeComponent();
    }

    #region All Widgets

    private async void AllWidgetsItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            var widgetId = WidgetProperties.GetId(element);
            var indexTag = await _widgetManagerService.AddWidgetAsync(widgetId, ViewModel.RefreshAddedWidget, false);
        }
    }

    #endregion

    #region Widget Items

    #region Context Menu

    private MenuFlyout GetRightClickMenu()
    {
        var menuFlyout = new MenuFlyout();

        var deleteMenuItem = new MenuFlyoutItem
        {
            Text = "MenuFlyoutItem_DeleteWidget.Text".GetLocalized()
        };
        deleteMenuItem.Click += (s, e) => DeleteWidget();
        menuFlyout.Items.Add(deleteMenuItem);

        return menuFlyout;
    }

    private void WidgetItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            _widgetId = WidgetProperties.GetId(element);
            _indexTag = WidgetProperties.GetIndexTag(element);
            RightClickMenu.ShowAt(element, new FlyoutShowOptions { Position = e.GetPosition(element) });
            e.Handled = true;
        }
    }

    private async void DeleteWidget()
    {
        if (_indexTag != -1)
        {
            if (await App.MainWindow.ShowDeleteWidgetDialogAsync() == WidgetDialogResult.Left)
            {
                ViewModel.RefreshDisabledWidget(_widgetId, _indexTag);
                await _widgetManagerService.DeleteWidgetAsync(_widgetId, _indexTag, false);
            }
            _indexTag = -1;
        }
    }

    #endregion

    #region Setting Page

    private void WidgetItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            var isEditable = WidgetProperties.GetEditable(element);
            if (!isEditable)
            {
                _indexTag = -1;
                return;
            }
            _widgetId = WidgetProperties.GetId(element);
            _indexTag = WidgetProperties.GetIndexTag(element);
            if (_indexTag != -1)
            {
                _widgetManagerService.NavigateToWidgetSettingPage(_widgetId, _indexTag);
                _indexTag = -1;
            }
        }
    }

    #endregion

    #endregion
}
