using System.Runtime.InteropServices;
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

    private static readonly Dictionary<Window, int> WindowsAndDispatcherThreads = [];

    public static void Initialize(DispatcherQueue dispatcherQueue)
    {
        MainDispatcherQueue = dispatcherQueue;
        MainDispatcherThreadId = Environment.CurrentManagedThreadId;
    }

    public static void RegisterWindow<T>(T window) where T : Window
    {
        var threadId = Environment.CurrentManagedThreadId;
        if (!WindowsAndDispatcherThreads.TryAdd(window, threadId))
        {
            WindowsAndDispatcherThreads[window] = threadId;
        }

        window.Closed += (sender, args) => UnregisterWindow(window);
    }

    private static void UnregisterWindow<T>(T window) where T : Window
    {
        WindowsAndDispatcherThreads.Remove(window);
    }

    private static bool IsDispatcherThreadDifferent<T>(this T window) where T : Window
    {
        return Environment.CurrentManagedThreadId != window.GetDispatcherThreadId();
    }

    private static int GetDispatcherThreadId<T>(this T window) where T : Window
    {
        return WindowsAndDispatcherThreads.FirstOrDefault(x => x.Key == window).Value;
    }

    #region ui thread extensions

    #region single window

    public static Task EnqueueOrInvokeAsync<T>(this T window, Func<T, Task> function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal) where T : Window
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
        }, typeof(COMException));
    }

    public static Task<T1?> EnqueueOrInvokeAsync<T, T1>(this T window, Func<T, Task<T1>> function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal) where T : Window
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
        }, typeof(COMException));
    }

    public static Task EnqueueOrInvokeAsync<T>(this T window, Action<T> function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal) where T : Window
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
        }, typeof(COMException));
    }

    public static Task<T1?> EnqueueOrInvokeAsync<T, T1>(this T window, Func<T, T1> function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal) where T : Window
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
        }, typeof(COMException));
    }

    #endregion

    #region multiple windows

    public static async Task EnqueueOrInvokeAsync<T>(this List<T> windows, Func<T, Task> function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal) where T : Window
    {
        var tasks = new List<Task>();

        foreach (var window in windows)
        {
            tasks.Add(window.EnqueueOrInvokeAsync(function, priority));
        }

        await Task.WhenAll(tasks);
    }

    public static async Task EnqueueOrInvokeAsync<T>(this List<T> windows, Action<T> function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal) where T : Window
    {
        var tasks = new List<Task>();

        foreach (var window in windows)
        {
            tasks.Add(window.EnqueueOrInvokeAsync(function, priority));
        }

        await Task.WhenAll(tasks);
    }

    #endregion

    #region ignore exceptions

    private static async Task<bool> IgnoreExceptions(Func<Task> action, Type? exceptionToIgnore = null)
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
                LogExtensions.LogInformation(string.Empty, ex, ex.Message);

                return false;
            }
            else
            {
                throw;
            }
        }
    }

    private static async Task<T?> IgnoreExceptions<T>(Func<Task<T>> action, Type? exceptionToIgnore = null)
    {
        try
        {
            return await action();
        }
        catch (Exception ex)
        {
            if (exceptionToIgnore is null || exceptionToIgnore.IsAssignableFrom(ex.GetType()))
            {
                LogExtensions.LogInformation(string.Empty, ex, ex.Message);

                return default;
            }
            else
            {
                throw;
            }
        }
    }

    #endregion

    #endregion

    #region dispatcher thread info

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