namespace DesktopWidgets3.Infrastructure;

public static class Constant
{
    public const string Widgets = "Widgets";

    public const string WidgetMetadataFileName = "widget.json";

    public static readonly string WidgetsPreinstalledDirectory = Path.Combine(AppContext.BaseDirectory, Widgets);
}
