using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace DesktopWidgets3.Views.Pages;

public sealed partial class WidgetStorePage : Page
{
    public WidgetStorePageViewModel ViewModel { get; }

    private readonly IWidgetResourceService _widgetResourceService = DependencyExtensions.GetRequiredService<IWidgetResourceService>();

    private readonly MenuFlyout InstallRightClickMenu;
    private readonly MenuFlyout UninstallRightClickMenu;

    private WidgetProviderType _providerType = WidgetProviderType.DesktopWidgets3;
    private string _widgetId = string.Empty;

    public WidgetStorePage()
    {
        ViewModel = DependencyExtensions.GetRequiredService<WidgetStorePageViewModel>();
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
            Text = "MenuFlyoutItem_InstallWidget.Text".GetLocalizedString()
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
            Text = "MenuFlyoutItem_UninstallWidget.Text".GetLocalizedString()
        };
        uninstallMenuItem.Click += (s, e) => UninstallWidget();
        menuFlyout.Items.Add(uninstallMenuItem);

        return menuFlyout;
    }

    private void AvailableWidgetStoreItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is WidgetStoreItem item)
        {
            _providerType = item.ProviderType;
            _widgetId = item.Id;
            InstallRightClickMenu.ShowAt(element, new FlyoutShowOptions { Position = e.GetPosition(element) });
            e.Handled = true;
        }
    }

    private void InstalledWidgetStoreItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is WidgetStoreItem item)
        {
            _providerType = item.ProviderType;
            _widgetId = item.Id;
            UninstallRightClickMenu.ShowAt(element, new FlyoutShowOptions { Position = e.GetPosition(element) });
            e.Handled = true;
        }
    }

    private async void InstallWidget()
    {
        if (_widgetId != string.Empty)
        {
            if (_providerType == WidgetProviderType.DesktopWidgets3)
            {
                await _widgetResourceService.InstallWidgetAsync(_widgetId);
                if (await DialogFactory.ShowRestartApplicationDialogAsync() == WidgetDialogResult.Left)
                {
                    App.RestartApplication();
                }
            }
            else
            {
                // TODO(Future): Implement GitHub widget installation, not supported yet.
            }

            _widgetId = string.Empty;
        }
    }

    private async void UninstallWidget()
    {
        if (_widgetId != string.Empty)
        {
            if (_providerType == WidgetProviderType.DesktopWidgets3)
            {
                await _widgetResourceService.UninstallWidgetAsync(_widgetId);
                if (await DialogFactory.ShowRestartApplicationDialogAsync() == WidgetDialogResult.Left)
                {
                    App.RestartApplication();
                }
            }
            else
            {
                await Launcher.LaunchUriAsync(new("ms-settings:appsfeatures"));
            }

            _widgetId = string.Empty;
        }
    }

    #endregion

    #endregion
}
