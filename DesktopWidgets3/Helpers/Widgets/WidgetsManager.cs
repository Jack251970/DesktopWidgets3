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
    }
}
