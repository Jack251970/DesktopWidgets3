namespace DesktopWidgets3.Core.Widgets.Models.WidgetContexts;

public class WidgetInfo : IWidgetInfo
{
    public required IWidgetContext WidgetContext { get; set; }

    public BaseWidgetSettings Settings => throw new NotImplementedException();
}
