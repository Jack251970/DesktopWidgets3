using H.NotifyIcon;
using Windows.UI.Composition;

namespace DesktopWidgets3.Views.Windows;

public sealed partial class FullScreenWindow : WindowEx
{
    public FullScreenWindow()
    {
        InitializeComponent();

        Title = string.Empty;

        IsTitleBarVisible = IsMaximizable = IsMaximizable = IsResizable = false;

        IsAlwaysOnTop = true;

        SystemBackdrop = new BlurredBackdrop();

        SystemHelper.HideWindowIconFromTaskbar(this.GetWindowHandle());
    }

    #region Hide & Show

    public void Hide()
    {
        this.Hide(false);
    }

    public void ShowFullScreen()
    {
        var primaryMonitorInfo = DisplayMonitor.GetPrimaryMonitorInfo();
        var primaryMonitorWidth = primaryMonitorInfo.RectMonitor.Width!.Value;
        var primaryMonitorHeight = primaryMonitorInfo.RectMonitor.Height!.Value;
        this.MoveAndResize(0, 0, primaryMonitorWidth, primaryMonitorHeight);
        this.Show(false);
    }

    #endregion

    private partial class BlurredBackdrop : CompositionBrushBackdrop
    {
        protected override CompositionBrush CreateBrush(Compositor compositor)
            => compositor.CreateHostBackdropBrush();
    }
}
