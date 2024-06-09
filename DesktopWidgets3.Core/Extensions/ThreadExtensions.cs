using System.Runtime.InteropServices;
using CommunityToolkit.WinUI;
using Microsoft.Extensions.Logging;
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

    private static readonly Dictionary<Window, int> WindowsAndDispatcherThreads = [];

    public static void Initialize(DispatcherQueue dispatcherQueue)
    {
        MainDispatcherQueue = dispatcherQueue;
        MainDispatcherThreadId = Environment.CurrentManagedThreadId;
    }

    public static void RegisterWindow(Window window)
    {
        var threadId = Environment.CurrentManagedThreadId;
        if (!WindowsAndDispatcherThreads.TryAdd(window, threadId))
        {
            WindowsAndDispatcherThreads[window] = threadId;
        }

        window.Closed += (sender, args) => UnregisterWindow(window);
    }

    private static void UnregisterWindow(Window window)
    {
        WindowsAndDispatcherThreads.Remove(window);
    }

    private static bool IsDispatcherThreadDifferent(this Window window)
    {
        return Environment.CurrentManagedThreadId != window.GetDispatcherThreadId();
    }

    private static int GetDispatcherThreadId(this Window window)
    {
        return WindowsAndDispatcherThreads.FirstOrDefault(x => x.Key == window).Value;
    }

    #region ui thread extensions

    // for single window

    public static Task EnqueueOrInvokeAsync(this Window window, Func<Window, Task> function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
    {
        return IgnoreExceptions(() =>
        {
            var dispatcher = window.DispatcherQueue;
            if (dispatcher is not null && window.IsDispatcherThreadDifferent())
            {
                return dispatcher.EnqueueAsync(() => function(window), priority);
            }
            else
            {
                return function(window);
            }
        }, null, typeof(COMException));
    }

    public static Task<T?> EnqueueOrInvokeAsync<T>(this Window window, Func<Window, Task<T>> function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
    {
        return IgnoreExceptions(() =>
        {
            var dispatcher = window.DispatcherQueue;
            if (dispatcher is not null && window.IsDispatcherThreadDifferent())
            {
                return dispatcher.EnqueueAsync(() => function(window), priority);
            }
            else
            {
                return function(window);
            }
        }, null, typeof(COMException));
    }

    public static Task EnqueueOrInvokeAsync(this Window window, Action<Window> function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
    {
        return IgnoreExceptions(() =>
        {
            var dispatcher = window.DispatcherQueue;
            if (dispatcher is not null && window.IsDispatcherThreadDifferent())
            {
                return dispatcher.EnqueueAsync(() => function(window), priority);
            }
            else
            {
                function(window);
                return Task.CompletedTask;
            }
        }, null, typeof(COMException));
    }

    public static Task<T?> EnqueueOrInvokeAsync<T>(this Window window, Func<Window, T> function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
    {
        return IgnoreExceptions(() =>
        {
            var dispatcher = window.DispatcherQueue;
            if (dispatcher is not null && window.IsDispatcherThreadDifferent())
            {
                return dispatcher.EnqueueAsync(() => function(window), priority);
            }
            else
            {
                return Task.FromResult(function(window));
            }
        }, null, typeof(COMException));
    }

    // for multiple windows

    public static Task EnqueueOrInvokeAsync(this List<Window> windows, Func<Window, Task> function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
    {
        return IgnoreExceptions(() =>
        {
            var (ThreadSameWindows, ThreadDifferentWindows) = windows.GetThreadInfo();

            var tasks = new List<Task>();

            foreach (var window in ThreadSameWindows)
            {
                tasks.Add(function(window));
            }

            foreach (var (dispatcherThreadInfo, windowList) in ThreadDifferentWindows)
            {
                var dispatcher = dispatcherThreadInfo.DispatcherQueue;

                if (dispatcher is not null)
                {
                    tasks.Add(dispatcher.EnqueueAsync(() =>
                    {
                        foreach (var window in windowList)
                        {
                            function(window);
                        }
                    }, priority));
                }
                else
                {
                    foreach (var window in windowList)
                    {
                        tasks.Add(function(window));
                    }
                }
            }

            return Task.WhenAll(tasks);
        }, null, typeof(COMException));
    }

    public static Task EnqueueOrInvokeAsync(this List<Window> windows, Action<Window> function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
    {
        return IgnoreExceptions(() =>
        {
            var (ThreadSameWindows, ThreadDifferentWindows) = windows.GetThreadInfo();

            foreach (var window in ThreadSameWindows)
            {
                function(window);
            }

            foreach (var (dispatcherThreadInfo, windowList) in ThreadDifferentWindows)
            {
                var dispatcher = dispatcherThreadInfo.DispatcherQueue;

                if (dispatcher is not null)
                {
                    dispatcher.EnqueueAsync(() =>
                    {
                        foreach (var window in windowList)
                        {
                            function(window);
                        }
                    }, priority);
                }
                else
                {
                    foreach (var window in windowList)
                    {
                        function(window);
                    }
                }
            }

            return Task.CompletedTask;
        }, null, typeof(COMException));
    }

    private static async Task<bool> IgnoreExceptions(Func<Task> action, ILogger? logger = null, Type? exceptionToIgnore = null)
    {
        try
        {
            await action();

            return true;
        }
        catch (Exception ex)
        {
            if (exceptionToIgnore is null || exceptionToIgnore.IsAssignableFrom(ex.GetType()))
            {
                logger?.LogInformation(ex, ex.Message);

                return false;
            }
            else
            {
                throw;
            }
        }
    }

    private static async Task<T?> IgnoreExceptions<T>(Func<Task<T>> action, ILogger? logger = null, Type? exceptionToIgnore = null)
    {
        try
        {
            return await action();
        }
        catch (Exception ex)
        {
            if (exceptionToIgnore is null || exceptionToIgnore.IsAssignableFrom(ex.GetType()))
            {
                logger?.LogInformation(ex, ex.Message);

                return default;
            }
            else
            {
                throw;
            }
        }
    }

    #endregion

    #region dispatcher thread info

    private static (List<Window> ThreadSameWindows, Dictionary<DispatcherThreadInfo, List<Window>> ThreadDifferentWindows) GetThreadInfo(this List<Window> windows)
    {
        var threadSameWindows = new List<Window>();
        var threadDifferentWindows = new Dictionary<DispatcherThreadInfo, List<Window>>();
        foreach (var window in windows)
        {
            var (isWindowThreadDifferent, dispatcherThreadInfo) = window.GetThreadInfo();
            if (isWindowThreadDifferent)
            {
                if (threadDifferentWindows.TryGetValue(dispatcherThreadInfo, out var value))
                {
                    value.Add(window);
                }
                else
                {
                    threadDifferentWindows.Add(dispatcherThreadInfo, [window]);
                }
            }
            else
            {
                threadSameWindows.Add(window);
            }
        }

        return (threadSameWindows, threadDifferentWindows);
    }

    private static (bool IsWindowThreadDifferent, DispatcherThreadInfo DispatcherThreadInfo) GetThreadInfo(this Window window)
    {
        var windowThreadId = window.GetDispatcherThreadId();
        return (Environment.CurrentManagedThreadId != windowThreadId, new DispatcherThreadInfo()
        {
            ThreadId = windowThreadId,
            DispatcherQueue = window.DispatcherQueue
        });
    }

    private class DispatcherThreadInfo
    {
        public int ThreadId { get; set; } = MainDispatcherThreadId;

        public DispatcherQueue DispatcherQueue { get; set; } = null!;

        public override bool Equals(object? obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var other = (DispatcherThreadInfo)obj;
            return ThreadId == other.ThreadId && DispatcherQueue == other.DispatcherQueue;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ThreadId, DispatcherQueue);
        }

        public static bool operator ==(DispatcherThreadInfo left, DispatcherThreadInfo right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(DispatcherThreadInfo left, DispatcherThreadInfo right)
        {
            return !Equals(left, right);
        }

        public override string ToString()
        {
            return $"ThreadId: {ThreadId}, DispatcherQueue: {DispatcherQueue}";
        }
    }

    #endregion
}