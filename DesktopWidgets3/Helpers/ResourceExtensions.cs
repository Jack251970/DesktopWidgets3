using System.Collections.Concurrent;
using Microsoft.Windows.ApplicationModel.Resources;

namespace DesktopWidgets3.Helpers;

public static class ResourceExtensions
{
    private static readonly ResourceMap resourcesTree = new ResourceManager().MainResourceMap.TryGetSubtree("Resources");

    private static readonly ConcurrentDictionary<string, string> cachedResources = new();

    public static string GetLocalized(this string resourceKey)
    {
        if (cachedResources.TryGetValue(resourceKey, out var value))
        {
            return value;
        }

        value = resourcesTree.TryGetValue(resourceKey)?.ValueAsString;

#if DEBUG
        if (value is null)
        {
            throw new Exception($"Resource key '{resourceKey}' not found.");
        }
        return cachedResources[resourceKey] = value;
#else
        return cachedResources[resourceKey] = value ?? string.Empty;
#endif
    }
}
