// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Windows.ApplicationModel;

namespace DevHome.Dashboard.Common.Services;

public interface IExtensionWrapper
{
    /// <summary>
    /// Gets the DisplayName of the package as mentioned in the manifest
    /// </summary>
    string PackageDisplayName { get; }

    /// <summary>
    /// Gets DisplayName of the extension as mentioned in the manifest
    /// </summary>
    string ExtensionDisplayName { get; }

    /// <summary>
    /// Gets PackageFullName of the extension
    /// </summary>
    string PackageFullName { get; }

    /// <summary>
    /// Gets PackageFamilyName of the extension
    /// </summary>
    string PackageFamilyName { get; }

    /// <summary>
    /// Gets Publisher of the extension
    /// </summary>
    string Publisher { get; }

    /// <summary>
    /// Gets class id (GUID) of the extension class (which implements IExtension) as mentioned in the manifest
    /// </summary>
    string ExtensionClassId { get; }

    /// <summary>
    /// Gets the date on which the application package was installed or last updated.
    /// </summary>
    DateTimeOffset InstalledDate { get; }

    /// <summary>
    /// Gets the PackageVersion of the extension
    /// </summary>
    PackageVersion Version { get; }

    /// <summary>
    /// Gets the Unique Id for the extension
    /// </summary>
    public string ExtensionUniqueId { get; }
}
