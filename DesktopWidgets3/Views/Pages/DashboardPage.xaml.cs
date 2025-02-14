﻿using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace DesktopWidgets3.Views.Pages;

public sealed partial class DashboardPage : Page
{
    public DashboardPageViewModel ViewModel { get; }

    private readonly IWidgetManagerService _widgetManagerService = DependencyExtensions.GetRequiredService<IWidgetManagerService>();

    private readonly MenuFlyout RightClickMenu;

    private WidgetProviderType _providerType = WidgetProviderType.DesktopWidgets3;
    private string _widgetId = string.Empty;
    private string _widgetType = string.Empty;
    private int _widgetIndex = -1;

    public DashboardPage()
    {
        ViewModel = DependencyExtensions.GetRequiredService<DashboardPageViewModel>();
        RightClickMenu = GetRightClickMenu();
        InitializeComponent();
    }

    private async void Page_ActualThemeChanged(FrameworkElement sender, object _)
    {
        await ViewModel.UpdateThemeAsync(sender.ActualTheme);
    }

    #region Widget Items

    #region Context Menu

    private MenuFlyout GetRightClickMenu()
    {
        var menuFlyout = new MenuFlyout();

        var deleteMenuItem = new MenuFlyoutItem
        {
            Text = "MenuFlyoutItem_DeleteWidget.Text".GetLocalizedString()
        };
        deleteMenuItem.Click += (s, e) => DeleteWidget();
        menuFlyout.Items.Add(deleteMenuItem);

        return menuFlyout;
    }

    private void WidgetItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is DashboardWidgetItem item)
        {
            _providerType = item.ProviderType;
            _widgetId = item.Id;
            _widgetType = item.Type;
            _widgetIndex = item.Index;
            RightClickMenu.ShowAt(element, new FlyoutShowOptions { Position = e.GetPosition(element) });
            e.Handled = true;
        }
    }

    private async void DeleteWidget()
    {
        if (_widgetIndex != -1)
        {
            if (await DialogFactory.ShowDeleteWidgetDialogAsync() == WidgetDialogResult.Left)
            {
                await ViewModel.RefreshDeletedWidgetAsync(_providerType, _widgetId, _widgetType, _widgetIndex);
                await _widgetManagerService.DeleteWidgetAsync(_providerType, _widgetId, _widgetType, _widgetIndex, false);
            }
            _widgetIndex = -1;
        }
    }

    #endregion

    #region Setting Page

    private void WidgetItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is DashboardWidgetItem item)
        {
            var providerType = item.ProviderType;
            if (providerType != WidgetProviderType.DesktopWidgets3)
            {
                return;
            }

            var isEditable = item.Editable;
            if (!isEditable)
            {
                _widgetIndex = -1;
                return;
            }

            _widgetId = item.Id;
            _widgetType = item.Type;
            _widgetIndex = item.Index;
            if (_widgetIndex != -1)
            {
                _widgetManagerService.NavigateToWidgetSettingPage(_widgetId, _widgetType, _widgetIndex);
                _widgetIndex = -1;
            }
        }
    }

    #endregion

    #endregion
}
