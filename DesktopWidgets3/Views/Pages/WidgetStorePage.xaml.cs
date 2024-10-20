using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace DesktopWidgets3.Views.Pages;

public sealed partial class WidgetStorePage : Page
{
    public WidgetStoreViewModel ViewModel { get; }

    private readonly IWidgetResourceService _widgetResourceService = DependencyExtensions.GetRequiredService<IWidgetResourceService>();

    private readonly MenuFlyout InstallRightClickMenu;
    private readonly MenuFlyout UninstallRightClickMenu;

    private string _widgetId = string.Empty;

    public WidgetStorePage()
    {
        ViewModel = DependencyExtensions.GetRequiredService<WidgetStoreViewModel>();
        InstallRightClickMenu = GetInstalledRightClickMenu();
        UninstallRightClickMenu = GetUninstalledRightClickMenu();
        InitializeComponent();
    }

    #region Widget Items

    #region Context Menu

    private MenuFlyout GetInstalledRightClickMenu()
    {
        var menuFlyout = new MenuFlyout();

        var installMenuItem = new MenuFlyoutItem
        {
            Text = "MenuFlyoutItem_InstallWidget.Text".GetLocalized()
        };
        installMenuItem.Click += (s, e) => InstallWidget();
        menuFlyout.Items.Add(installMenuItem);

        return menuFlyout;
    }

    private MenuFlyout GetUninstalledRightClickMenu()
    {
        var menuFlyout = new MenuFlyout();

        var uninstallMenuItem = new MenuFlyoutItem
        {
            Text = "MenuFlyoutItem_UninstallWidget.Text".GetLocalized()
        };
        uninstallMenuItem.Click += (s, e) => UninstallWidget();
        menuFlyout.Items.Add(uninstallMenuItem);

        return menuFlyout;
    }

    private void AvailableWidgetStoreItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is WidgetStoreItem item)
        {
            _widgetId = item.Id;
            InstallRightClickMenu.ShowAt(element, new FlyoutShowOptions { Position = e.GetPosition(element) });
            e.Handled = true;
        }
    }

    private void InstalledWidgetStoreItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is WidgetStoreItem item)
        {
            _widgetId = item.Id;
            UninstallRightClickMenu.ShowAt(element, new FlyoutShowOptions { Position = e.GetPosition(element) });
            e.Handled = true;
        }
    }

    private async void InstallWidget()
    {
        if (_widgetId != string.Empty)
        {
            await _widgetResourceService.InstallWidgetAsync(_widgetId);
            _widgetId = string.Empty;
        }
    }

    private async void UninstallWidget()
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
