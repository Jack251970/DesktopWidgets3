// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Dashboard.Common.Services;
using DevHome.Dashboard.Models;
using Serilog;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppExtensions;
using Windows.Foundation.Collections;

namespace DevHome.Dashboard.Services;

public partial class ExtensionService : IExtensionService
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(ExtensionService));

    private const string MicrosoftWidgetAppExtensionHost = "com.microsoft.windows.widgets";

    private const string WidgetProviderProperty = "WidgetProvider";
    private const string ActivationProperty = "Activation";
    private const string CreateInstanceProperty = "CreateInstance";
    private const string ClassIdProperty = "@ClassId";

    public event EventHandler<List<IExtensionWrapper>>? OnExtensionsChanged;

    public event EventHandler<IExtensionWrapper>? OnPackageInstalled;
    public event EventHandler<string>? OnPackageUninstalled;
    public event EventHandler<IExtensionWrapper>? OnPackageUpdated;

    private readonly PackageCatalog _catalog = PackageCatalog.OpenForCurrentUser();
    private readonly SemaphoreSlim _catalogLock = new(1, 1);

    private readonly SemaphoreSlim _getInstalledExtensionsLock = new(1, 1);

    private readonly List<IExtensionWrapper> _installedExtensions = [];

    public ExtensionService()
    {
        _catalog.PackageInstalling += Catalog_PackageInstalling;
        _catalog.PackageUninstalling += Catalog_PackageUninstalling;
        _catalog.PackageUpdating += Catalog_PackageUpdating;
    }

    #region Package Catalog Events

    private async void Catalog_PackageInstalling(PackageCatalog sender, PackageInstallingEventArgs args)
    {
        if (args.IsComplete)
        {
            await _catalogLock.WaitAsync();
            try
            {
                var isDevHomeExtension = await Task.Run(() =>
                {
                    return IsValidDevHomeExtension(args.Package);
                });

                if (isDevHomeExtension)
                {
                    var package = await Task.Run(() =>
                    {
                        return OnPackageChangeAsync(args.Package);
                    });

                    if (package != null)
                    {
                        OnPackageInstalled?.Invoke(this, package);
                    }
                }
            }
            finally
            {
                _catalogLock.Release();
            }
        }
    }

    private async void Catalog_PackageUninstalling(PackageCatalog sender, PackageUninstallingEventArgs args)
    {
        if (args.IsComplete)
        {
            await _catalogLock.WaitAsync();
            try
            {
                foreach (var extension in _installedExtensions)
                {
                    if (extension.PackageFullName == args.Package.Id.FullName)
                    {
                        var package = await Task.Run(() =>
                        {
                            return OnPackageChangeAsync(args.Package);
                        });

                        if (package == null)
                        {
                            OnPackageUninstalled?.Invoke(this, extension.PackageFamilyName);
                        }

                        break;
                    }
                }
            }
            finally
            {
                _catalogLock.Release();
            }
        }
    }

    private async void Catalog_PackageUpdating(PackageCatalog sender, PackageUpdatingEventArgs args)
    {
        if (args.IsComplete)
        {
            await _catalogLock.WaitAsync();
            try
            {
                var isDevHomeExtension = await Task.Run(() =>
                {
                    return IsValidDevHomeExtension(args.TargetPackage);
                });

                if (isDevHomeExtension)
                {
                    var package = await Task.Run(() =>
                    {
                        return OnPackageChangeAsync(args.TargetPackage);
                    });

                    if (package != null)
                    {
                        OnPackageUpdated?.Invoke(this, package);
                    }
                }
            }
            finally
            {
                _catalogLock.Release();
            }
        }
    }

    private async Task<IExtensionWrapper?> OnPackageChangeAsync(Package package)
    {
        // Clear the cache of installed extensions and widgets
        _installedExtensions.Clear();

        // Get the extension
        await GetInstalledExtensionsAsync();

        // Invoke the event
        OnExtensionsChanged?.Invoke(this, _installedExtensions);

        // Get the widget
        foreach (var extension in _installedExtensions)
        {
            if (extension.PackageFullName == package.Id.FullName)
            {
                return extension;
            }
        }

        return null;
    }

    #endregion

    public async Task<List<IExtensionWrapper>> GetInstalledExtensionsAsync()
    {
        await _getInstalledExtensionsLock.WaitAsync();
        try
        {
            if (_installedExtensions.Count == 0)
            {
                var extensions = await GetInstalledAppExtensionsAsync();
                foreach (var extension in extensions)
                {
                    var (widgetProvider, classIds) = await GetMicrosoftExtensionPropertiesAsync(extension);
                    if (widgetProvider == null || classIds.Count == 0)
                    {
                        continue;
                    }

                    foreach (var classId in classIds)
                    {
                        try
                        {
                            var extensionWrapper = new ExtensionWrapper(extension, classId);
                            _installedExtensions.Add(extensionWrapper);
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex, "Error creating ExtensionWrapper for extension {ExtensionId}", extension.Id);
                        }
                    }
                }
            }

            return _installedExtensions;
        }
        finally
        {
            _getInstalledExtensionsLock.Release();
        }
    }

    public IExtensionWrapper? GetInstalledExtension(string extensionUniqueId)
    {
        var extension = _installedExtensions.Where(extension => extension.ExtensionUniqueId.Equals(extensionUniqueId, StringComparison.Ordinal));
        return extension.FirstOrDefault();
    }

    private static async Task<bool> IsValidDevHomeExtension(Package package)
    {
        var extensions = await AppExtensionCatalog.Open(MicrosoftWidgetAppExtensionHost).FindAllAsync();
        foreach (var extension in extensions)
        {
            if (package.Id?.FullName == extension.Package?.Id?.FullName)
            {
                var (widgetProvider, classId) = await GetMicrosoftExtensionPropertiesAsync(extension);
                return widgetProvider != null && classId.Count != 0;
            }
        }

        return false;
    }

    private static async Task<(IPropertySet?, List<string>)> GetMicrosoftExtensionPropertiesAsync(AppExtension extension)
    {
        var classIds = new List<string>();
        var properties = await extension.GetExtensionPropertiesAsync();

        if (properties is null)
        {
            return (null, classIds);
        }

        var widgetProvider = GetSubPropertySet(properties, WidgetProviderProperty);
        if (widgetProvider is null)
        {
            return (null, classIds);
        }

        var activation = GetSubPropertySet(widgetProvider, ActivationProperty);
        if (activation is null)
        {
            return (widgetProvider, classIds);
        }

        // Handle case where extension creates multiple instances.
        classIds.AddRange(GetCreateInstanceList(activation));

        return (widgetProvider, classIds);
    }

    private static async Task<IEnumerable<AppExtension>> GetInstalledAppExtensionsAsync()
    {
        return await AppExtensionCatalog.Open(MicrosoftWidgetAppExtensionHost).FindAllAsync();
    }

    private static IPropertySet? GetSubPropertySet(IPropertySet propSet, string name)
    {
        return propSet.TryGetValue(name, out var value) ? value as IPropertySet : null;
    }

    private static object[]? GetSubPropertySetArray(IPropertySet propSet, string name)
    {
        return propSet.TryGetValue(name, out var value) ? value as object[] : null;
    }

    /// <summary>
    /// There are cases where the extension creates multiple COM instances.
    /// </summary>
    /// <param name="activationPropSet">Activation property set object</param>
    /// <returns>List of ClassId strings associated with the activation property</returns>
    private static List<string> GetCreateInstanceList(IPropertySet activationPropSet)
    {
        var propSetList = new List<string>();
        var singlePropertySet = GetSubPropertySet(activationPropSet, CreateInstanceProperty);
        if (singlePropertySet != null)
        {
            var classId = GetProperty(singlePropertySet, ClassIdProperty);

            // If the instance has a classId as a single string, then it's only supporting a single instance.
            if (classId != null)
            {
                propSetList.Add(classId);
            }
        }
        else
        {
            var propertySetArray = GetSubPropertySetArray(activationPropSet, CreateInstanceProperty);
            if (propertySetArray != null)
            {
                foreach (var prop in propertySetArray)
                {
                    if (prop is not IPropertySet propertySet)
                    {
                        continue;
                    }

                    var classId = GetProperty(propertySet, ClassIdProperty);
                    if (classId != null)
                    {
                        propSetList.Add(classId);
                    }
                }
            }
        }

        return propSetList;
    }

    private static string? GetProperty(IPropertySet propSet, string name)
    {
        return propSet[name] as string;
    }

    #region IDisposable

    private bool _disposed;

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _catalog.PackageInstalling -= Catalog_PackageInstalling;
                _catalog.PackageUninstalling -= Catalog_PackageUninstalling;
                _catalog.PackageUpdating -= Catalog_PackageUpdating;
                _installedExtensions.Clear();
                _catalogLock.Dispose();
                _getInstalledExtensionsLock.Dispose();
            }

            _disposed = true;
        }
    }

    #endregion
}
