using Microsoft.UI.Dispatching;

namespace DesktopWidgets3.Widget.Models.Parameter;

public class WidgetViewModelNavigationParameter
{
    public required string Id { get; set; }

    public required int IndexTag { get; set; }

    public required DispatcherQueue DispatcherQueue { get; set; }

    public required BaseWidgetSettings Settings { get; set; }
}
