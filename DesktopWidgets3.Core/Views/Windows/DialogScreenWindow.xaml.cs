using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Windows.UI.Composition;
using WinUIEx;

namespace DesktopWidgets3.Core.Views.Windows;

/// <summary>
/// An empty window that can be used for showing message dialog on full screen.
/// </summary>
public sealed partial class DialogScreenWindow : WindowEx
{
    public DialogScreenWindow()
    {
        InitializeComponent();

        Title = string.Empty;

        SystemBackdrop = new BlurredBackdrop();

        Activated += DialogScreenWindow_Activated;
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
        {
            WindowExtensions.Move(this, -10000, -10000);
            Activate();
        });
    }

    private void DialogScreenWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        Activated -= DialogScreenWindow_Activated;
        this.Hide(); // Hides at the first time
        var hwnd = this.GetWindowHandle();
        SystemHelper.HideWindowFromTaskbar(hwnd); // Hide the window from the taskbar
        HwndExtensions.SetWindowStyle(hwnd, WindowStyle.Border); // Set the window style to Border
    }

    /// <summary>
    /// Show the window full in the primary monitor.
    /// </summary>
    /// <returns>True if the window is shown successfully.</returns>
    public bool Show()
    {
        var primaryMonitorInfo = DisplayMonitor.GetPrimaryMonitorInfo();
        var primaryMonitorWidth = primaryMonitorInfo.RectMonitor.Width;
        var primaryMonitorHeight = primaryMonitorInfo.RectMonitor.Height;
        var scale = 96f / this.GetDpiForWindow();
        if (primaryMonitorWidth != null && primaryMonitorHeight != null)
        {
            this.MoveAndResize(0, 0, (double)primaryMonitorWidth * scale + 1, (double)primaryMonitorHeight * scale + 1);
            WindowExtensions.Show(this);
            return true;
        }
        return false;
    }

    #region Backdrop

    private partial class BlurredBackdrop : CompositionBrushBackdrop
    {
        protected override CompositionBrush CreateBrush(Compositor compositor)
            => compositor.CreateHostBackdropBrush();
    }

    #endregion
}
