using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Widget.Models.Parameter;

public class WidgetNavigationParameter
{
    public required Window Window { get; set; }

    public required BaseWidgetSettings Settings { get; set; }
}
