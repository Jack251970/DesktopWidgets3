﻿using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Views.Pages.Widgets.Settings;

public sealed partial class PerformanceSettingsPage : Page
{
    public PerformanceSettingsViewModel ViewModel { get; }

    public PerformanceSettingsPage()
    {
        ViewModel = App.GetService<PerformanceSettingsViewModel>();
        InitializeComponent();
    }
}
