namespace DesktopWidgets3.Widget.Models.Main;

public class WidgetInitContext(WidgetMetadata metadata, IPublicAPIService api)
{
    public WidgetMetadata WidgetMetadata { get; private set; } = metadata;

    public IPublicAPIService API { get; private set; } = api;
}
