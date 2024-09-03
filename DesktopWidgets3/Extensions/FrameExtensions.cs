using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.Extensions;

internal static class FrameExtensions
{
    public static object? GetPageViewModel(this Frame frame) => 
        frame?.Content?.GetType().GetProperty("ViewModel")?.GetValue(frame.Content, null);
}
