using Windows.UI.Composition;
using WinUIEx;

namespace DesktopWidgets3.Core.Views.Windows;

/// <summary>
/// An empty window that can be used for showing message dialog on full screen.
/// </summary>
public sealed partial class DialogScreen : NoChromeWindow
{
    public DialogScreen()
    {
        InitializeComponent();

        Title = string.Empty;

        SystemBackdrop = new BlurredBackdrop();

        SystemHelper.HideWindowIconFromTaskbar(this.GetWindowHandle());
    }

    #region Show

    public void MoveFullScreen()
    {
        var primaryMonitorInfo = DisplayMonitor.GetPrimaryMonitorInfo();
        var primaryMonitorWidth = primaryMonitorInfo.RectMonitor.Width;
        var primaryMonitorHeight = primaryMonitorInfo.RectMonitor.Height;
        var scale = 96f / this.GetDpiForWindow();
        if (primaryMonitorWidth != null && primaryMonitorHeight != null)
        {
            this.MoveAndResize(0, 0, (double)primaryMonitorWidth * scale + 1, (double)primaryMonitorHeight * scale + 1);
        }
    }

    #endregion

    #region Backdrop

    private partial class BlurredBackdrop : CompositionBrushBackdrop
    {
        protected override CompositionBrush CreateBrush(Compositor compositor)
            => compositor.CreateHostBackdropBrush();
    }

    #endregion
}
