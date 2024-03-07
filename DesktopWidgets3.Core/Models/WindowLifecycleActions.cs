using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Core.Models;

public class WindowLifecycleActions
{
    public Action? Window_Creating { get; set; }

    public Action<Window>? Window_Created { get; set; }

    public Action<Window>? Window_Closing { get; set; }

    public Action? Window_Closed { get; set; }
}
