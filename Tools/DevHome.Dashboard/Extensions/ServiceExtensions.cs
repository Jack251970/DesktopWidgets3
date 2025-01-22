// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Dashboard.Common.Services;
using DevHome.Dashboard.Services;
using DevHome.Dashboard.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DevHome.Dashboard.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddDashboard(this IServiceCollection services)
    {
        // DI factory pattern for creating instances with certain parameters
        // determined at runtime
        services.AddSingleton<WidgetViewModelFactory>(
            sp => (widget, widgetSize, widgetDefinition) =>
                ActivatorUtilities.CreateInstance<WidgetViewModel>(sp, widget, widgetSize, widgetDefinition));

        // Services
        services.AddSingleton<IWidgetServiceService, WidgetServiceService>();
        services.AddSingleton<IWidgetHostingService, WidgetHostingService>();
        services.AddSingleton<IWidgetIconService, WidgetIconService>();
        services.AddSingleton<IWidgetScreenshotService, WidgetScreenshotService>();
        services.AddSingleton<IAdaptiveCardRenderingService, WidgetAdaptiveCardRenderingService>();

        return services;
    }
}
