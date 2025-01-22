// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Dashboard.Services.Core.Contracts;
using DevHome.Dashboard.Services.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DevHome.Dashboard.Services.Core.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        services.TryAddSingleton<IMicrosoftStoreService, MicrosoftStoreService>();
        services.TryAddSingleton<IPackageDeploymentService, PackageDeploymentService>();
        return services;
    }
}
