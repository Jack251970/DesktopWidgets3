namespace DesktopWidgets3.Widget.Models.Main;

public class WidgetInitContext
{
    public WidgetInitContext()
    {
    }

    public WidgetInitContext(WidgetMetadata metadata, IPublicAPIService api)
    {
        WidgetMetadata = metadata;
        API = api;
    }

    public WidgetMetadata WidgetMetadata { get; private set; } = null!;

    public IPublicAPIService API { get; set; } = null!;
}
