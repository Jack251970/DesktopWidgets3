using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;

namespace DesktopWidgets3.Core.Extensions;

/// <summary>
/// Provides static extension for UI elements, e.g. Windows.
/// </summary>
public static class WindowsExtensions
{
    public static WindowEx MainWindow { get; private set; } = null!;

    private static readonly Dictionary<Window, WindowLifecycleHandler> WindowsAndLifecycle = [];

    private static IWindowService? FallbackWindowService;

    public static void Initialize(IWindowService windowService)
    {
        FallbackWindowService = windowService;
    }

    public static List<Window> GetAllWindows()
    {
        return new List<Window>(WindowsAndLifecycle.Keys);
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

                // signal that window closing is complete
                lifecycleActions?.CompletionSource?.SetResult();
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

        if (type == ActivationType.Main)
        {
            // register main window in ui element extension
            if (MainWindow is null)
            {
                MainWindow = window as WindowEx ?? throw new InvalidOperationException("MainWindow must be of type WindowEx.");
            }
            else
            {
                throw new InvalidOperationException("MainWindow can only be initialized once.");
            }
        }
        else
        {
            // register non-main window in ui element extension
            var lifecycleHandler = new WindowLifecycleHandler
            {
                ExitDeferral = deferral,
                LifecycleActions = lifecycleActions ?? new()
            };
            RegisterWindow(window, lifecycleHandler);
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

        // register window in ui thread extension
        ThreadExtensions.RegisterWindow(window);

        return window;
    }

    public static async Task CloseWindow(Window window)
    {
        var lifecycleHandler = WindowsAndLifecycle.TryGetValue(window, out var value) ? value : null;
        if (lifecycleHandler?.ExitDeferral is not null)
        {
            // initialize task completion source
            lifecycleHandler.LifecycleActions.CompletionSource ??= new();

            // start dispatch complete deferral
            lifecycleHandler.ExitDeferral.Complete();

            // wait for task completion source to complete
            await lifecycleHandler.LifecycleActions.CompletionSource.Task;
        }
        else
        {
            // invoke action before window closing
            lifecycleHandler?.LifecycleActions.Window_Closing?.Invoke(window);

            window.Close();

            // invoke action after window closing
            lifecycleHandler?.LifecycleActions.Window_Closed?.Invoke();
        }
    }

    public static async Task CloseAllWindows()
    {
        foreach (var window in WindowsAndLifecycle.Keys)
        {
            await CloseWindow(window);
        }
    }

    private static void RegisterWindow(Window window, WindowLifecycleHandler lifecycleHandler)
    {
        if (WindowsAndLifecycle.TryAdd(window, lifecycleHandler))
        {
            window.Closed += (sender, args) => UnregisterWindow(window);
        }
    }

    private static void UnregisterWindow(Window window)
    {
        WindowsAndLifecycle.Remove(window);
    }
}

public enum ActivationType
{
    None,
    Main,
    Widget,
    Overlay,
    Blank
}
