// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.Dashboard.Common.Services;

public interface IExtensionService : IDisposable
{
    Task<List<IExtensionWrapper>> GetInstalledExtensionsAsync();

    IExtensionWrapper? GetInstalledExtension(string extensionUniqueId);

    event EventHandler<List<IExtensionWrapper>> OnExtensionsChanged;

    event EventHandler<IExtensionWrapper>? OnPackageInstalled;

    event EventHandler<string>? OnPackageUninstalled;

    event EventHandler<IExtensionWrapper>? OnPackageUpdated;
}
