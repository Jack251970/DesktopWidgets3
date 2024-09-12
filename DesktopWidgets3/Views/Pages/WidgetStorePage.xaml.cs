using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace DesktopWidgets3.Views.Pages;

public sealed partial class WidgetStorePage : Page
{
    public WidgetStoreViewModel ViewModel { get; }

    private readonly IWidgetResourceService _widgetResourceService = DependencyExtensions.GetRequiredService<IWidgetResourceService>();

    private string _widgetId = string.Empty;

    public WidgetStorePage()
    {
        ViewModel = DependencyExtensions.GetRequiredService<WidgetStoreViewModel>();
        InitializeComponent();
    }

    #region Widget Items

    #region Context Menu

    private void WidgetStoreItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            _widgetId = WidgetProperties.GetId(element);
            element.ContextFlyout.ShowAt(element, new FlyoutShowOptions { Position = e.GetPosition(element) });
            e.Handled = true;
        }
    }

    private async void MenuFlyoutItemInstallWidget_Click(object sender, RoutedEventArgs e)
    {
        if (_widgetId != string.Empty)
        {
            await _widgetResourceService.InstallWidgetAsync(_widgetId);
            _widgetId = string.Empty;
        }
    }

    private async void MenuFlyoutItemUninstallWidget_Click(object sender, RoutedEventArgs e)
    {
        if (_widgetId != string.Empty)
        {
            await _widgetResourceService.UninstallWidgetAsync(_widgetId);
            _widgetId = string.Empty;
        }
    }

    #endregion

    #endregion
}
