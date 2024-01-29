using System.Collections.Concurrent;
using Microsoft.Windows.ApplicationModel.Resources;

namespace DesktopWidgets3.Core.Extensions;

/// <summary>
/// Provides static extension for resources.
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
        var cachedResourceKey = $"{resourceFileName}/{resourceKey}";
        if (cachedResources.TryGetValue(cachedResourceKey, out var value))
        {
            return value;
        }

        var resourcesTree = resourcesTrees[resourceFileName];
        value = resourcesTree.TryGetValue(resourceKey)?.ValueAsString;

        // TODO: Check string here.
        return cachedResources[cachedResourceKey] = value ?? string.Empty;

#if DEBUG
        if (value is null)
        {
            throw new Exception($"Resource key '{cachedResourceKey}' not found.");
        }
        return cachedResources[cachedResourceKey] = value;
#else
            return cachedResources[cachedResourceKey] = value ?? string.Empty;
#endif
    }
}
