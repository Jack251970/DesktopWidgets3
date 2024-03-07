using CommunityToolkit.WinUI;

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;

namespace DesktopWidgets3.Core.Extensions;

/// <summary>
/// Provides static extension for UI elements.
/// </summary>
public static class UIElementExtensions
{
    private static IWindowService? FallbackWindowService;

    public static readonly List<Window> WindowInstances = new();

    public static void Initialize(IWindowService windowService)
    {
        FallbackWindowService = windowService;
    }

    public static async Task<T> CreateWindow<T>(ActivationType type, object? parameter = null, bool newThread = false) where T : Window, new()
    {
        if (newThread)
        {
            T window = null!;

            var tcs = new TaskCompletionSource<T>();

            var thread = new Thread(async () =>
            {
                // create a DispatcherQueue on this new thread
                var dq = DispatcherQueueController.CreateOnCurrentThread();

                // initialize xaml in it
                WindowsXamlManager.InitializeForCurrentThread();

                // create a new window
                window = await CreateWindow<T>(type, parameter);

                // complete the task with the window object
                tcs.SetResult(window);

                // run message pump
                dq.DispatcherQueue.RunEventLoop();
            })
            {
                // will be destroyed when main is closed
                IsBackground = true
            };

            thread.Start();

            return await tcs.Task;
        }
        else
        {
            // create a new window
            var window = await CreateWindow<T>(type, parameter);

            return window;
        }
    }

    private static async Task<T> CreateWindow<T>(ActivationType type, object? parameter = null) where T : Window, new()
    {
        if (FallbackWindowService is null)
        {
            throw new InvalidOperationException("Window service is not initialized.");
        }

        // create a new window
        var window = new T();

        // register window
        RegisterWindow(window);

        switch (type)
        {
            case ActivationType.None:
                break;
            case ActivationType.Widget:
                await FallbackWindowService.ActivateWidgetWindowAsync(window);
                break;
            case ActivationType.Overlay:
                await FallbackWindowService.ActivateOverlayWindowAsync(window);
                break;
            default:
                await FallbackWindowService.ActivateBlankWindowAsync(window, parameter);
                break;
        } 

        return window;
    }

    public static void RegisterWindow(Window window)
    {
        if (!WindowInstances.Contains(window))
        {
            WindowInstances.Add(window);
            window.Closed += (sender, args) => UnregisterWindow(window);
        }
    }

    public static void UnregisterWindow(Window window)
    {
        if (WindowInstances.Contains(window))
        {
            WindowInstances.Remove(window);
        }
    }

    public static bool CheckWindowClosed(Window window)
    {
        return !WindowInstances.Contains(window);
    }

    public static void CloseAllWindows()
    {
        var windowInstances = WindowInstances.ToList();
        foreach (var window in windowInstances)
        {
            window.Close();
        }
    }

    #region extensions

    public static Task EnqueueOrInvokeAsync(this DispatcherQueue? dispatcher, bool newThread, Func<Task> function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
    {
        if (newThread && dispatcher is not null)
        {
            return dispatcher.EnqueueAsync(function, priority);
        }
        else
        {
            return function();
        }
    }

    public static Task<T> EnqueueOrInvokeAsync<T>(this DispatcherQueue? dispatcher, bool newThread, Func<Task<T>> function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
    {
        if (newThread && dispatcher is not null)
        {
            return dispatcher.EnqueueAsync(function, priority);
        }
        else
        {
            return function();
        }
    }

    public static Task EnqueueOrInvokeAsync(this DispatcherQueue? dispatcher, bool newThread, Action function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
    {
        if (newThread && dispatcher is not null)
        {
            return dispatcher.EnqueueAsync(function, priority);
        }
        else
        {
            function();
            return Task.CompletedTask;
        }
    }

    public static Task<T> EnqueueOrInvokeAsync<T>(this DispatcherQueue? dispatcher, bool newThread, Func<T> function, DispatcherQueuePriority priority = DispatcherQueuePriority.Normal)
    {
        if (newThread && dispatcher is not null)
        {
            return dispatcher.EnqueueAsync(function, priority);
        }
        else
        {
            return Task.FromResult(function());
        }
    }

    #endregion
}

public enum ActivationType
{
    None,
    Widget,
    Overlay,
    Blank
}
