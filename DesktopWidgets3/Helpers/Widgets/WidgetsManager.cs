namespace DesktopWidgets3.Helpers.Widgets;

public static class WidgetsManager
{
    public static List<WidgetPair> AllWidgets { get; private set; } = [];

    public static List<WidgetMetadata> AllWidgetsMetadata { get; private set; } = [];

    private static readonly string[] Directories =
    {
        Constant.WidgetsPreinstalledDirectory
    };

    public static void LoadWidgets()
    {
        AllWidgetsMetadata = WidgetsConfig.Parse(Directories);
        AllWidgets = WidgetsLoader.Widgets(AllWidgetsMetadata);
        InstallResourceFiles(AllWidgetsMetadata);
    }

    #region Xaml Resources

    private static void InstallResourceFiles(List<WidgetMetadata> widgetsMetadata)
    {
        foreach (var metadata in widgetsMetadata)
        {
            InstallResourceFiles(metadata);
        }
    }

    private static void InstallResourceFiles(WidgetMetadata metadata)
    {
        var widgetDirectory = metadata.WidgetDirectory;
        var xbfFiles = Directory.EnumerateFiles(widgetDirectory, "*.xbf", SearchOption.AllDirectories);
        var resourceFiles = xbfFiles;

        foreach (var resourceFile in resourceFiles)
        {
            var relativePath = Path.GetRelativePath(widgetDirectory, resourceFile);
            // TODO: Initialize AppContext.BaseDirector in Constants.
            var destinationPath = Path.Combine(AppContext.BaseDirectory, relativePath);

            var destinationDirectory = Path.GetDirectoryName(destinationPath);
            if (!(string.IsNullOrEmpty(destinationDirectory) || Directory.Exists(destinationDirectory)))
            {
                Directory.CreateDirectory(destinationDirectory);
            }
            File.Copy(resourceFile, destinationPath, true);
        }
    }

    #endregion
}
