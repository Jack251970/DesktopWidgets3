// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Dashboard.Common.Services;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppExtensions;

namespace DevHome.Dashboard.Models;

public class ExtensionWrapper(AppExtension appExtension, string classId) : IExtensionWrapper
{
    public string PackageDisplayName { get; } = appExtension.Package.DisplayName;

    public string ExtensionDisplayName { get; } = appExtension.DisplayName;

    public string PackageFullName { get; } = appExtension.Package.Id.FullName;

    public string PackageFamilyName { get; } = appExtension.Package.Id.FamilyName;

    public string ExtensionClassId { get; } = classId ?? throw new ArgumentNullException(nameof(classId));

    public string Publisher { get; } = appExtension.Package.PublisherDisplayName;

    public DateTimeOffset InstalledDate { get; } = appExtension.Package.InstalledDate;

    public PackageVersion Version { get; } = appExtension.Package.Id.Version;

    /// <summary>
    /// Gets the unique id for this Dev Home extension. The unique id is a concatenation of:
    /// <list type="number">
    /// <item>The AppUserModelId (AUMID) of the extension's application. The AUMID is the concatenation of the package
    /// family name and the application id and uniquely identifies the application containing the extension within
    /// the package.</item>
    /// <item>The Extension Id. This is the unique identifier of the extension within the application.</item>
    /// </list>
    /// </summary>
    public string ExtensionUniqueId { get; } = appExtension.AppInfo.AppUserModelId + "!" + appExtension.Id;
}
