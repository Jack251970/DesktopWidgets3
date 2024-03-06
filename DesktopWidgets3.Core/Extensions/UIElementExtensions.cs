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

    public static async Task<T> CreateWindow<T>(bool newThread) where T : Window, new()
    {
        if (newThread)
        {
            T window = null!;

            var tcs = new TaskCompletionSource<T>();

            var thread = new Thread(state =>
            {
                // create a DispatcherQueue on this new thread
                var dq = DispatcherQueueController.CreateOnCurrentThread();

                // initialize xaml in it
                WindowsXamlManager.InitializeForCurrentThread();

                // create a new window
                window = CreateWindow<T>();

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
            var window = CreateWindow<T>();

            return window;
        }
    }

    private static T CreateWindow<T>() where T : Window, new()
    {
        if (FallbackWindowService is null)
        {
            throw new InvalidOperationException("Window service is not initialized.");
        }

        // create a new window
        var window = new T();

        // register window
        RegisterWindow(window);

        if (window is BlankWindow blankWindow)
        {
            // activate window using fallback service
            FallbackWindowService.ActivateBlankWindow(blankWindow, false);
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
}
