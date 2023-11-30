﻿using Microsoft.UI.Xaml.Controls;

using DesktopWidgets3.ViewModels.Pages.Widget.Clock;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using DesktopWidgets3.Contracts.Services;

namespace DesktopWidgets3.Views.Pages.Widget.Clock;

public sealed partial class ClockPage : Page
{
    public ClockViewModel ViewModel
    {
        get;
    }

    public ClockPage()
    {
        ViewModel = App.GetService<ClockViewModel>();
        InitializeComponent();
    }

    private void MenuFlyoutItemEnterEidtMode_Click(object sender, RoutedEventArgs e)
    {
        App.GetService<IWidgetManagerService>().EnterEditMode();
    }

    private void MenuFlyoutItemExitEditModeAndSave_Click(object sender, RoutedEventArgs e)
    {
        App.GetService<IWidgetManagerService>().ExitEditModeAndSave();
    }

    private void ContentArea_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        var element = sender as FrameworkElement;
        if (element != null)
        {
            element.ContextFlyout.ShowAt(element, new FlyoutShowOptions { Position = e.GetPosition(element) });
            e.Handled = true;
        }
    }
}
