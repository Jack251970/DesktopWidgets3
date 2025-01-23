using Microsoft.UI.Xaml.Media;

namespace DesktopWidgets3.Core.Widgets.Models.WidgetItems;

public class BaseWidgetStoreItem : BaseWidgetGroupItem
{
    public required string Version { get; set; }
}

public class JsonWidgetStoreItem : BaseWidgetStoreItem
{
    public required bool IsPreinstalled { get; set; }

    public required bool IsInstalled { get; set; }

    public required string ResourcesFolder { get; set; }
}

public class WidgetStoreItem : BaseWidgetStoreItem
{
    public required WidgetProviderType ProviderType { get; set; }

    public required string FamilyName { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    public required string Author { get; set; }

    public required string Website { get; set; }

    public required Brush IconFill { get; set; }
}
