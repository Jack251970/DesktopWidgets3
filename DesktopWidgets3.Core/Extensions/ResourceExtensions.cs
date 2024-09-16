﻿// Copyright (c) 2024 Jack251970
// Licensed under the GPL License. See the LICENSE.

using Microsoft.Windows.ApplicationModel.Resources;

using System.Collections.Concurrent;
using System.Reflection;

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
        { Constant.DefaultResourceFileName, HostResourceMap.TryGetSubtree(Constant.DefaultResourceFileName) }
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

    #endregion

    #region extension methods

    public static string GetLocalized(this string resourceKey, string resourceFileName = Constant.DefaultResourceFileName)
    {
        // Fix resource key
        resourceKey = resourceKey.Replace(".", "/");

        // Try to get cached value
        var cachedResourceKey = $"{resourceFileName}/{resourceKey}";
        if (cachedResources.TryGetValue(cachedResourceKey, out var value))
        {
            return value;
        }

        // Get resource value
        var resourcesTree = resourcesTrees[resourceFileName];
        value = resourcesTree.TryGetValue(resourceKey)?.ValueAsString;

        // Return empty string if the resource key is not found.
        return cachedResources[cachedResourceKey] = value ?? string.Empty;
    }

    #endregion
}
