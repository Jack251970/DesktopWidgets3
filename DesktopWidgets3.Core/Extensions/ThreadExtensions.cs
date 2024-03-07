using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Core.Extensions;

/// <summary>
/// Provides static extension for thread & UI thread.
/// </summary>
public static class ThreadExtensions
{
    public static DispatcherQueue? MainDispatcherQueue { get; private set; }
    public static int MainDispatcherThreadId { get; private set; }

    private static readonly Dictionary<int, List<Window>> WindowsOnThreads = new();

    public static void Initialize(DispatcherQueue dispatcherQueue)
    {
        MainDispatcherQueue = dispatcherQueue;
        MainDispatcherThreadId = Environment.CurrentManagedThreadId;
    }

    public static void RegisterWindow(Window window)
    {
        var threadId = Environment.CurrentManagedThreadId;
        if (!WindowsOnThreads.TryGetValue(threadId, out var value))
        {
            WindowsOnThreads.Add(threadId, new List<Window>() { window });
            window.Closed += (sender, args) => UnregisterWindow(window, threadId);
        }
        else
        {
            value.Add(window);
        }
    }

    private static void UnregisterWindow(Window window, int threadId)
    {
        if (WindowsOnThreads.TryGetValue(threadId, out var value))
        {
            value.Remove(window);
        }
    }
}
