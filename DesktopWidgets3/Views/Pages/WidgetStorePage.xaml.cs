using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace DesktopWidgets3.Views.Pages;

public sealed partial class WidgetStorePage : Page
{
    public WidgetStoreViewModel ViewModel { get; }

    private string _widgetId = string.Empty;
    private bool _isPreinstalled = false;

    public WidgetStorePage()
    {
        ViewModel = App.GetService<WidgetStoreViewModel>();
        InitializeComponent();
    }

    #region Widget Items

    #region Context Menu

    private void WidgetStoreItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            _widgetId = WidgetProperties.GetId(element);
            _isPreinstalled = WidgetProperties.GetIsPreinstalled(element);
            element.ContextFlyout.ShowAt(element, new FlyoutShowOptions { Position = e.GetPosition(element) });
            e.Handled = true;
        }
    }

    private void MenuFlyoutItemInstallWidget_Click(object sender, RoutedEventArgs e)
    {
        if (_widgetId != string.Empty)
        {
            // TODO
            _widgetId = string.Empty;
        }
    }

    private void MenuFlyoutItemUninstallWidget_Click(object sender, RoutedEventArgs e)
    {
        if (_widgetId != string.Empty)
        {
            // TODO
            _widgetId = string.Empty;
        }
    }

    #endregion

    #endregion
}
