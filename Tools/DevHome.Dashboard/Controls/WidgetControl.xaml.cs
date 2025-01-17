// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
//using DevHome.Common.Extensions;
//using DevHome.Common.Services;
using DevHome.Dashboard.ComSafeWidgetObjects;
using DevHome.Dashboard.Helpers;
using DevHome.Dashboard.Services;
using DevHome.Dashboard.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Widgets;
using Serilog;
using Windows.UI.ViewManagement;

namespace DevHome.Dashboard.Controls;

[ObservableObject]
public sealed partial class WidgetControl : UserControl
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(WidgetControl));

    private readonly DispatcherQueue _dispatcherQueue = DependencyExtensions.GetRequiredService<DispatcherQueue>();
    private readonly IWidgetHostingService _widgetHostingService = DependencyExtensions.GetRequiredService<IWidgetHostingService>();
    private readonly IWidgetIconService _widgetIconService = DependencyExtensions.GetRequiredService<IWidgetIconService>();

    // TODO(Future): Add support for TextScaleFactorChanged.
    private readonly UISettings _uiSettings = new();

    private SelectableMenuFlyoutItem? _currentSelectedSize;

    [ObservableProperty]
    private double _widgetHeight;

    [ObservableProperty]
    private double _widgetWidth;

    public WidgetViewModel WidgetSource
    {
        get => (WidgetViewModel)GetValue(WidgetSourceProperty);
        set
        {
            SetValue(WidgetSourceProperty, value);
            if (WidgetSource != null)
            {
                SetScaledWidthAndHeight(_uiSettings.TextScaleFactor);

                // When the WidgetViewModel is updated, the widget icon must also be also updated.
                // Since the icon update must happen asynchronously on the UI thread, it must be
                // called in code rather than binding.
                //UpdateWidgetHeaderIconFillAsync();
            }
        }
    }

    public static readonly DependencyProperty WidgetSourceProperty = DependencyProperty.Register(
        nameof(WidgetSource), typeof(WidgetViewModel), typeof(WidgetControl), new PropertyMetadata(null));

    public WidgetControl()
    {
        InitializeComponent();
    }

    [RelayCommand]
    private void OnLoaded()
    {
        _uiSettings.TextScaleFactorChanged += HandleTextScaleFactorChangedAsync;
    }

    [RelayCommand]
    private void OnUnloaded()
    {
        _uiSettings.TextScaleFactorChanged -= HandleTextScaleFactorChangedAsync;
        WidgetSource = null!;
    }

    private async void HandleTextScaleFactorChangedAsync(UISettings sender, object args)
    {
        await _dispatcherQueue.EnqueueAsync(() =>
        {
            if (WidgetSource == null)
            {
                return;
            }

            SetScaledWidthAndHeight(sender.TextScaleFactor);
        });
    }

    private void SetScaledWidthAndHeight(double textScale)
    {
        //(WidgetWidth, WidgetHeight) = WidgetHelpers.GetScaledWidthAndHeight(WidgetSource.WidgetSize/*, textScale*/);
    }
}
