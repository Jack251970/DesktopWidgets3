// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Dashboard.Services;
using Microsoft.UI.Xaml;

namespace DevHome.Dashboard.ViewModels;

// TODO(Future): Remove this class and merge it to MicrosoftWidgetModel.
public partial class DashboardViewModel : ObservableObject
{
    public IWidgetHostingService WidgetHostingService { get; }

    public IWidgetIconService WidgetIconService { get; }

    public IWidgetScreenshotService WidgetScreenshotService { get; }

    [ObservableProperty]
    public bool _isLoading;

    [ObservableProperty]
    private bool _hasWidgetServiceInitialized;

    public DashboardViewModel(
        IWidgetHostingService widgetHostingService,
        IWidgetIconService widgetIconService,
        IWidgetScreenshotService widgetScreenshotService)
    {
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
