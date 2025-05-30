﻿// Copyright (c) 2024 Jack251970
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using Microsoft.Windows.ApplicationModel.Resources;

namespace DesktopWidgets3.Core.Extensions;

/// <summary>
/// Provides static extension for resources management, support string caching.
/// </summary>
public static class ResourceExtensions
{
    private static readonly ConcurrentDictionary<string, string> cachedResources = new();

    private static readonly ResourceMap HostResourceMap = new ResourceManager().MainResourceMap;

    private static readonly Dictionary<string, ResourceMap> resourcesTrees = new()
    {
        { Constants.DefaultResourceFileName, HostResourceMap.TryGetSubtree(Constants.DefaultResourceFileName) }
    };

    #region resource management

    /// <summary>
    /// Add resource file of the host project.
    /// </summary>
    /// <param name="resourceFileName">
    /// The name of the resource file.
    /// </param>
    public static void AddLocalResource(string resourceFileName)
    {
        var resourceMap = HostResourceMap.TryGetSubtree(resourceFileName);
        resourcesTrees.Add(resourceFileName, resourceMap);
    }

    /// <summary>
    /// Add resource file of a inner project.
    /// </summary>
    /// <param name="projectName">
    /// The project name of the inner project.
    /// </param>
    public static void AddInnerResource(string projectName)
    {
        var resourcePath = Path.Combine(AppContext.BaseDirectory, $"{projectName}.pri");
        var resourceMap = new ResourceManager(resourcePath).MainResourceMap.TryGetSubtree($"{projectName}/{Constants.DefaultResourceFileName}");
        resourcesTrees.Add(projectName, resourceMap);
    }

    /// <summary>
    /// Add resource file of a extension project.
    /// </summary>
    /// <param name="assembly">
    /// The assembly of the extension project.
    /// </param>
    public static void AddExternalResource(Assembly assembly)
    {
        var assemblyName = assembly.GetName().Name;
        var resourceMap = ApplicationExtensionHost.GetWinResourceMapForAssembly(assembly);
        if (assemblyName != null && resourceMap != null)
        {
            resourcesTrees.Add(assemblyName, resourceMap);
        }
    }

    /// <summary>
    /// Get resource map by resource file name.
    /// </summary>
    /// <param name="resourceFileName">
    /// The name of the resource file or the assembly of the extension project.
    /// </param>
    /// <returns></returns>
    public static ResourceMap? TryGetResourceMap(string resourceFileName = Constants.DefaultResourceFileName)
    {
        return resourcesTrees.TryGetValue(resourceFileName, out var resourceMap) ? resourceMap : null;
    }

    #endregion

    #region extension methods

    /// <summary>
    /// Gets the localized string of a resource key.
    /// </summary>
    /// <param name="resourceKey">Resource key.</param>
    /// <param name="resourceFileName">
    /// Resource file name. It can be the resource file name of the host project, 
    /// the project name of the inner project, or the assembly name of the extension project.
    /// </param>
    /// <param name="args">Placeholder arguments.</param>
    /// <returns>Localized value, or resource key if the value is empty or an exception occurred.</returns>
    public static string GetLocalizedString(this string resourceKey, string resourceFileName = Constants.DefaultResourceFileName, params object[] args)
    {
        // Fix resource key
        resourceKey = resourceKey.Replace(".", "/");

        // Try to get cached value
        var cachedResourceKey = $"{resourceFileName}/{resourceKey}";
        if (cachedResources.TryGetValue(cachedResourceKey, out var value))
        {
            return GetLocalizedString(value, args);
        }

        // Get resource value
        var resourcesTree = resourcesTrees[resourceFileName];
        value = resourcesTree.TryGetValue(resourceKey)?.ValueAsString;

        // Return empty string if the resource key is not found
        var returnValue = cachedResources[cachedResourceKey] = value ?? string.Empty;
        return GetLocalizedString(returnValue, args);
    }

    private static string GetLocalizedString(string value, params object[] args)
    {
        try
        {
            // only replace the placeholders if args is not empty
            if (args.Length > 0)
            {
                value = string.Format(CultureInfo.CurrentCulture, value, args);
            }
        }
        catch
        {
            value = string.Empty;
        }

        return value;
    }

    #endregion
}
