namespace DesktopWidgets3.Core.Widgets.Models.WidgetContexts;

public class WidgetContext : IWidgetContext
{
    public required string Id { get; set; }

    public required string Type { get; set; }

    public bool IsActive => throw new NotImplementedException();
}
