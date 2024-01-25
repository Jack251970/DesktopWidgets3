using Microsoft.UI.Dispatching;

namespace DesktopWidgets3.Core.Extensions;

/// <summary>
/// Provides static extension for dispatcher queue.
/// </summary>
public static class DispatcherExtensions
{
    private static DispatcherQueue? FallbackDispatcherQueue;

    private static bool _isInitialized;

    public static DispatcherQueue? DispatcherQueue => FallbackDispatcherQueue;

    public static void Initialize(DispatcherQueue dispatcherQueue)
    {
        if (!_isInitialized)
        {
            FallbackDispatcherQueue = dispatcherQueue;

            _isInitialized = true;
        }
    }
}
