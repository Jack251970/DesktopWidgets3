using Microsoft.UI.Xaml.Controls;

using DesktopWidgets3.ViewModels.Pages.Widget;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Helpers;
using Microsoft.UI.Xaml;
using DesktopWidgets3.Views.Windows;

namespace DesktopWidgets3.Views.Pages.Widget;

public sealed partial class FrameShellPage : Page
{
    public FrameShellViewModel ViewModel
    {
        get;
    }

    public Frame NavigationFrame
    {
        get;
    }

    private WidgetWindow WidgetWindow
    {
        get;
    }

    public FrameShellPage(FrameShellViewModel viewModel, IWidgetManagerService widgetManagerService)
    {
        ViewModel = viewModel;
        InitializeComponent();

        NavigationFrame = WidgetNavigationFrame;
        ViewModel.WidgetNavigationService.Frame = WidgetNavigationFrame;
        WidgetWindow = widgetManagerService.GetLastWidgetWindow();

        SetCustomTitleBar(false);
    }

    public void SetCustomTitleBar(bool customTitleBar)
    {
        WidgetWindow.ExtendsContentIntoTitleBar = customTitleBar;
        WidgetWindow.SetTitleBar(customTitleBar ? WidgetTitleBar : null);
        WidgetWindow.InitializeTitleBar();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        TitleBarHelper.UpdateTitleBar(RequestedTheme);
    }
}
