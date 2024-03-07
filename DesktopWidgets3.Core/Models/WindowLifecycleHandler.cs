using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Core.Models;

public class WindowLifecycleHandler
{
    // exit deferral for pass dispatching exit signal to the new thread
    public DispatcherExitDeferral? ExitDeferral { get; private set; }

    public Action? Window_Creating { get; set; }

    public Action<Window>? Window_Created { get; set; }

    public Action<Window>? Window_Closing { get; set; }

    public Action? Window_Closed { get; set; }

    public WindowLifecycleHandler()
    {

    }

    public WindowLifecycleHandler(DispatcherExitDeferral exitDeferral)
    {
        ExitDeferral = exitDeferral;
    }
}
