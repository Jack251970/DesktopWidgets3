using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;

namespace DesktopWidgets3.Files.App.Extensions;

// Window.DispatcherQueue seems to be null sometimes.
// We don't know why, but as a workaround, we invoke the function directly if DispatcherQueue is null.
public static class DispatcherQueueExtensions
{
    public static Task EnqueueOrInvokeAsync(this DispatcherQueue? dispatcher, Func<Task> function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
    {
        return dispatcher is not null ? dispatcher.EnqueueAsync(function, priority) : function();
    }

    public static Task<T> EnqueueOrInvokeAsync<T>(this DispatcherQueue? dispatcher, Func<Task<T>> function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
    {
        return dispatcher is not null ? dispatcher.EnqueueAsync(function, priority) : function();
    }

    public static Task EnqueueOrInvokeAsync(this DispatcherQueue? dispatcher, Action function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
    {
        if (dispatcher is not null)
        {
            return dispatcher.EnqueueAsync(function, priority);
        }
        else
        {
            function();
            return Task.CompletedTask;
        }
    }

    public static Task<T> EnqueueOrInvokeAsync<T>(this DispatcherQueue? dispatcher, Func<T> function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
    {
        return dispatcher is not null ? dispatcher.EnqueueAsync(function, priority) : Task.FromResult(function());
    }
}