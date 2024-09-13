using Microsoft.Windows.ApplicationModel.Resources;

using System.Collections.Concurrent;

namespace DesktopWidgets3.Core.Extensions;

/// <summary>
/// Provides static extension for resources management, support string caching.
/// </summary>
public static class ResourceExtensions
{
    private static readonly string DefaultResourceFileName = "Resources";

    private static readonly ConcurrentDictionary<string, string> cachedResources = new();

    private static readonly Dictionary<string, ResourceMap> resourcesTrees = new()
    {
        { DefaultResourceFileName, new ResourceManager().MainResourceMap.TryGetSubtree(DefaultResourceFileName) }
    };

    public static void AddStringResource(string resourceFileName)
    {
        var resourceMap = new ResourceManager().MainResourceMap.TryGetSubtree(resourceFileName);
        resourcesTrees.Add(resourceFileName, resourceMap);
    }

    public static string GetLocalized(this string resourceKey, string resourceFileName = "Resources")
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
}
