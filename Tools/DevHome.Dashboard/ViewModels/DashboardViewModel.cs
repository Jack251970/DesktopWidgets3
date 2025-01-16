﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Dashboard.Services;
using Microsoft.UI.Xaml;

namespace DevHome.Dashboard.ViewModels;

// TODO: Remove this.
public partial class DashboardViewModel : ObservableObject
{
    /*public IWidgetServiceService WidgetServiceService { get; }*/

    public IWidgetHostingService WidgetHostingService { get; }

    public IWidgetIconService WidgetIconService { get; }

    public IWidgetScreenshotService WidgetScreenshotService { get; }

    [ObservableProperty]
    public bool _isLoading;

    [ObservableProperty]
    private bool _hasWidgetServiceInitialized;

    public DashboardViewModel(
        // TODO: Add support for widgetServiceService.
        /*IWidgetServiceService widgetServiceService,*/
        IWidgetHostingService widgetHostingService,
        IWidgetIconService widgetIconService,
        IWidgetScreenshotService widgetScreenshotService)
    {
        /*WidgetServiceService = widgetServiceService;*/
        WidgetHostingService = widgetHostingService;
        WidgetIconService = widgetIconService;
        WidgetScreenshotService = widgetScreenshotService;
    }

    public Visibility GetNoWidgetMessageVisibility(int widgetCount, bool isLoading)
    {
        return (widgetCount == 0 && !isLoading && HasWidgetServiceInitialized) ? Visibility.Visible : Visibility.Collapsed;
    }

    public bool IsRunningElevated()
    {
        return RuntimeHelper.IsCurrentProcessRunningElevated();
    }
}
