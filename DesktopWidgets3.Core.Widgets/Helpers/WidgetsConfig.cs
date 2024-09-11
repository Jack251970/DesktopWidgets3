using System.Text.Json;

namespace DesktopWidgets3.Core.Widgets.Helpers;

public static class WidgetsConfig
{
    private static string ClassName => typeof(WidgetsConfig).Name;

    public static List<WidgetMetadata> Parse(string[] widgetDirectories, string preinstalledWidgetDirectory)
    {
        var directories = widgetDirectories.SelectMany(Directory.EnumerateDirectories);
        var nonPreinstalledMetadata = directories != null ? Parse(directories, false) : [];
        var preinstalledDirectories = Directory.EnumerateDirectories(preinstalledWidgetDirectory);
        var preinstalledMetadata = preinstalledDirectories != null ? Parse(preinstalledDirectories, true) : [];
        return [.. preinstalledMetadata, .. nonPreinstalledMetadata];
    }

    private static List<WidgetMetadata> Parse(IEnumerable<string> directories, bool preinstalled)
    {
        var allWidgetMetadata = new List<WidgetMetadata>();

        // FlowLauncherTODO: use linq when diable widget is implmented since parallel.foreach + list is not thread saft
        foreach (var directory in directories)
        {
            if (File.Exists(Path.Combine(directory, "NeedDelete.txt")))
            {
                try
                {
                    Directory.Delete(directory, true);
                }
                catch (Exception e)
                {
                    LogExtensions.LogError(ClassName, e, $"Can't delete <{directory}>");
                }
            }
            else
            {
                var metadata = GetWidgetMetadata(directory, preinstalled);
                if (metadata != null)
                {
                    allWidgetMetadata.Add(metadata);
                }
            }
        }

        (var uniqueList, var duplicateList) = GetUniqueLatestWidgetMetadata(allWidgetMetadata);

        duplicateList
            .ForEach(
                x => LogExtensions.LogWarning(ClassName, "GetUniqueLatestWidgetMetadata",
                $"Duplicate widget name: {x.Name}, id: {x.ID}, version: {x.Version} not loaded due to version not the highest of the duplicates"));

        return uniqueList;
    }

    private static (List<WidgetMetadata>, List<WidgetMetadata>) GetUniqueLatestWidgetMetadata(List<WidgetMetadata> allWidgetMetadata)
    {
        var duplicate_list = new List<WidgetMetadata>();
        var unique_list = new List<WidgetMetadata>();

        var duplicateGroups = allWidgetMetadata.GroupBy(x => x.ID).Where(g => g.Count() > 1).Select(y => y).ToList();

        foreach (var metadata in allWidgetMetadata)
        {
            var duplicatesExist = false;
            foreach (var group in duplicateGroups)
            {
                if (metadata.ID == group.Key)
                {
                    duplicatesExist = true;

                    // If metadata's version greater than each duplicate's version, CompareTo > 0
                    var count = group.Where(x => metadata.Version.CompareTo(x.Version) > 0).Count();

                    // Only add if the meatadata's version is the highest of all duplicates in the group
                    if (count == group.Count() - 1)
                    {
                        unique_list.Add(metadata);
                    }
                    else
                    {
                        duplicate_list.Add(metadata);
                    }
                }
            }

            if (!duplicatesExist)
            {
                unique_list.Add(metadata);
            }
        }

        return (unique_list, duplicate_list);
    }

    private static WidgetMetadata? GetWidgetMetadata(string widgetDirectory, bool preinstalled)
    {
        var configPath = Path.Combine(widgetDirectory, Constant.WidgetMetadataFileName);
        if (!File.Exists(configPath))
        {
            LogExtensions.LogError(ClassName, $"Didn't find config file <{configPath}>");
            return null;
        }

        WidgetMetadata? metadata;
        try
        {
            metadata = JsonSerializer.Deserialize<WidgetMetadata>(File.ReadAllText(configPath));
            metadata!.WidgetDirectory = widgetDirectory;
            metadata.Preinstalled = preinstalled;
        }
        catch (Exception e)
        {
            LogExtensions.LogError(ClassName, e, $"invalid json for config <{configPath}>");
            return null;
        }

        if (!AllowedLanguage.IsAllowed(metadata.Language))
        {
            LogExtensions.LogError(ClassName, $"Invalid language <{metadata.Language}> for config <{configPath}>");
            return null;
        }

        if (!File.Exists(metadata.ExecuteFilePath))
        {
            LogExtensions.LogError(ClassName, $"execute file path didn't exist <{metadata.ExecuteFilePath}> for conifg <{configPath}");
            return null;
        }

        return metadata;
    }
}
