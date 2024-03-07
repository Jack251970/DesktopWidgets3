using Microsoft.UI.Dispatching;

namespace DesktopWidgets3.Core.Extensions;

/// <summary>
/// Provides static extension for dispatcher queue.
/// </summary>
public static class UIThreadExtensions
{
    public static DispatcherQueue? MainDispatcherQueue { get; private set; }

    public static void Initialize(DispatcherQueue dispatcherQueue)
    {
        MainDispatcherQueue = dispatcherQueue;
    }
}
