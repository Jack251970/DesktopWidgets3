using System.Collections.Concurrent;
using Microsoft.Windows.ApplicationModel.Resources;

namespace DesktopWidgets3.Core.Extensions;

/// <summary>
/// Provides static extension for string localization.
/// </summary>
public static class LocalizationExtensions
{
    private static readonly ResourceMap resourcesTree = new ResourceManager().MainResourceMap.TryGetSubtree("Resources");

    private static readonly ConcurrentDictionary<string, string> cachedResources = new();

    public static string ToLocalized(this string resourceKey)
    {
        if (cachedResources.TryGetValue(resourceKey, out var value))
        {
            return value;
        }

        value = resourcesTree.TryGetValue(resourceKey)?.ValueAsString;

        // TODO: Check string here.
        return cachedResources[resourceKey] = value ?? string.Empty;

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
