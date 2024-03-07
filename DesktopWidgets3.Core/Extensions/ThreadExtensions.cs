using CommunityToolkit.WinUI;
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

    private static readonly Dictionary<Window, int> WindowsAndThreads = new();

    public static void Initialize(DispatcherQueue dispatcherQueue)
    {
        MainDispatcherQueue = dispatcherQueue;
        MainDispatcherThreadId = Environment.CurrentManagedThreadId;
    }

    public static void RegisterWindow(Window window)
    {
        var threadId = Environment.CurrentManagedThreadId;
        if (!WindowsAndThreads.ContainsKey(window))
        {
            WindowsAndThreads.Add(window, threadId);
        }
        else
        {
            WindowsAndThreads[window] = threadId;
        }
        window.Closed += (sender, args) => UnregisterWindow(window);
    }

    private static void UnregisterWindow(Window window)
    {
        WindowsAndThreads.Remove(window);
    }

    #region ui thread functions

    public static Task EnqueueOrInvokeAsync(this Window window, Func<Task> function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
    {
        var dispatcher = window.DispatcherQueue;
        if (dispatcher is not null && NeedChangeThread(window))
        {
            return dispatcher.EnqueueAsync(function, priority);
        }
        else
        {
            return function();
        }
    }

    public static Task<T> EnqueueOrInvokeAsync<T>(this Window window, Func<Task<T>> function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
    {
        var dispatcher = window.DispatcherQueue;
        if (dispatcher is not null && NeedChangeThread(window))
        {
            return dispatcher.EnqueueAsync(function, priority);
        }
        else
        {
            return function();
        }
    }

    public static Task EnqueueOrInvokeAsync(this Window window, Action function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
    {
        var dispatcher = window.DispatcherQueue;
        if (dispatcher is not null && NeedChangeThread(window))
        {
            return dispatcher.EnqueueAsync(function, priority);
        }
        else
        {
            function();
            return Task.CompletedTask;
        }
    }

    public static Task<T> EnqueueOrInvokeAsync<T>(this Window window, Func<T> function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
    {
        var dispatcher = window.DispatcherQueue;
        if (dispatcher is not null && NeedChangeThread(window))
        {
            return dispatcher.EnqueueAsync(function, priority);
        }
        else
        {
            return Task.FromResult(function());
        }
    }

    private static bool NeedChangeThread(Window window)
    {
        var currentThread = Environment.CurrentManagedThreadId;
        var windowThread = WindowsAndThreads.FirstOrDefault(x => x.Key == window).Value;
        return currentThread != windowThread;
    }

    #endregion
}
