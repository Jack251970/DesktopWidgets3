﻿using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Views.Pages;

public sealed partial class NavShellPage : Page
{
    public NavShellViewModel ViewModel { get; }

    public NavShellPage()
    {
        ViewModel = DependencyExtensions.GetRequiredService<NavShellViewModel>();
        InitializeComponent();

        ViewModel.NavigationService.Frame = NavigationFrame;
        ViewModel.ShellService.Initialize(NavigationViewControl);

        // A custom title bar is required for full window theme and Mica support.
        // https://docs.microsoft.com/windows/apps/develop/title-bar?tabs=winui3#full-customization
        App.MainWindow.ExtendsContentIntoTitleBar = true;
        App.MainWindow.SetTitleBar(AppTitleBar);
        App.MainWindow.TitleBar = AppTitleBar;

        AppTitleBarText.Text = ConstantHelper.AppDisplayName;

        App.MainWindow.Activated += MainWindow_Activated;
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        App.MainWindow.Activated -= MainWindow_Activated;
        App.MainWindow.TitleBarText = AppTitleBarText;
    }

    private void NavigationViewControl_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
    {
        AppTitleBar.Margin = new Thickness()
        {
            Left = sender.CompactPaneLength * (sender.DisplayMode == NavigationViewDisplayMode.Minimal ? 2 : 1),
            Top = AppTitleBar.Margin.Top,
            Right = AppTitleBar.Margin.Right,
            Bottom = AppTitleBar.Margin.Bottom
        };
    }
}
