using Microsoft.UI.Xaml;

using UnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;

namespace DesktopWidgets3.Core.Extensions;

/// <summary>
/// Provides static extension for application process.
/// </summary>
public static class ApplicationExtensions
{
    public static Action<object, UnhandledExceptionEventArgs>? UnhandledException { get; set; }

    public static Action<object, WindowEventArgs>? MainWindow_Hiding { get; set; }

    public static Action<object, WindowEventArgs>? MainWindow_Hided { get; set; }

    public static Action<object, WindowEventArgs>? MainWindow_Closing { get; set; }

    public static Action<object, WindowEventArgs>? MainWindow_Closed_Widgets_Closing { get; set; }

    public static Action<object, WindowEventArgs>? MainWindow_Closed_Widgets_Closed { get; set; }
}
