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
    private static readonly Dictionary<Window, DispatcherExitDeferral?> WindowInstances = new();

    private static IWindowService? FallbackWindowService;

    public static void Initialize(IWindowService windowService)
    {
        FallbackWindowService = windowService;
    }

    public static List<Window> GetAllWindows()
    {
        return WindowInstances.Keys.ToList();
    }

    public static async Task<T> GetWindow<T>(ActivationType type, object? parameter = null, bool isNewThread = false, WindowLifecycleActions? lifecycleActions = null) where T : Window, new()
    {
        T window = null!;
        DispatcherExitDeferral? deferral = null;

        if (isNewThread)
        {
            deferral = new DispatcherExitDeferral();

            var signal = new ManualResetEvent(false);

            var thread = new Thread(async () =>
            {
                // create a DispatcherQueue on this new thread
                var dq = DispatcherQueueController.CreateOnCurrentThread();

                // initialize xaml in it
                WindowsXamlManager.InitializeForCurrentThread();

                // invoke action before window creation
                lifecycleActions?.Window_Creating?.Invoke();

                // create a new window
                window = await GetWindow<T>(type, parameter);

                // invoke action after window creation
                lifecycleActions?.Window_Created?.Invoke(window);

                // signal that window creation is complete
                signal.Set();

                // run message pump
                dq.DispatcherQueue.RunEventLoop(DispatcherRunOptions.None, deferral);

                // invoke action before window closing
                lifecycleActions?.Window_Closing?.Invoke(window);

                // close window
                window.Close();

                // invoke action after window closing
                lifecycleActions?.Window_Closed?.Invoke();
            })
            {
                // will be destroyed when main is closed
                IsBackground = true
            };

            thread.Start();

            // wait for the signal
            signal.WaitOne();
        }
        else
        {
            // invoke action before window creation
            lifecycleActions?.Window_Creating?.Invoke();

            // create a new window
            window = await GetWindow<T>(type, parameter);

            // invoke action after window creation
            lifecycleActions?.Window_Created?.Invoke(window);
        }

        // register non-main window
        if (type != ActivationType.Main)
        {
            RegisterWindow(window, deferral);
        }

        return window;
    }

    private static async Task<T> GetWindow<T>(ActivationType type, object? parameter = null) where T : Window, new()
    {
        if (FallbackWindowService is null)
        {
            throw new InvalidOperationException("Window service is not initialized.");
        }

        // create a new window
        var window = new T();

        switch (type)
        {
            case ActivationType.None:
            case ActivationType.Main:
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

    public static void RegisterWindow(Window window, DispatcherExitDeferral? deferral)
    {
        if (!WindowInstances.ContainsKey(window))
        {
            WindowInstances.Add(window, deferral);
            window.Closed += (sender, args) => UnregisterWindow(window);
        }
    }

    public static void UnregisterWindow(Window window)
    {
        if (WindowInstances.ContainsKey(window))
        {
            WindowInstances.Remove(window);
        }
    }

    public static bool CheckWindowClosed(Window window)
    {
        return !WindowInstances.ContainsKey(window);
    }

    public static void CloseWindow(Window window)
    {
        var exitDeferral = WindowInstances[window];
        if (exitDeferral is null)
        {
            window.Close();
        }
        else
        {
            exitDeferral.Complete();
        }
    }

    public static void CloseAllWindows()
    {
        foreach (var window in WindowInstances.Keys)
        {
            CloseWindow(window);
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
    Main,
    Widget,
    Overlay,
    Blank
}
