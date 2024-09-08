namespace DesktopWidgets3.Widget.Models.Main;

public class WidgetInitContext
{
    public WidgetInitContext()
    {
    }

    public WidgetInitContext(WidgetMetadata metadata, IPublicAPI api)
    {
        WidgetMetadata = metadata;
        API = api;
    }

    public WidgetMetadata WidgetMetadata { get; private set; } = null!;

    public IPublicAPI API { get; set; } = null!;
}
