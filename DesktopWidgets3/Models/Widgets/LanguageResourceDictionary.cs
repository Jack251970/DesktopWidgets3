using System.Collections.Concurrent;
using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Models.Widgets;

/// <summary>
/// Language resource dictionary.
/// </summary>
internal class LanguageResourceDictionary : ResourceDictionary
{
    public LanguageResourceDictionary(Dictionary<string, string> dictionary)
    {
        foreach (var key in dictionary.Keys)
        {
            Add(key, dictionary[key]);
        }
    }

    public LanguageResourceDictionary(ConcurrentDictionary<string, string> dictionary)
    {
        foreach (var key in dictionary.Keys)
        {
            Add(key, dictionary[key]);
        }
    }
}
