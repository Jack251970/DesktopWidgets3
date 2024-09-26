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

    public void Show()
    {
        ShowFullScreen();
    }

    public void Hide()
    {
        this.Hide(false);
    }

    private void ShowFullScreen()
    {
        var primaryMonitorInfo = DisplayMonitor.GetPrimaryMonitorInfo();
        var primaryMonitorWidth = primaryMonitorInfo.RectMonitor.Width;
        var primaryMonitorHeight = primaryMonitorInfo.RectMonitor.Height;
        var scale = 96f / this.GetDpiForWindow();
        if (primaryMonitorWidth != null && primaryMonitorHeight != null)
        {
            this.MoveAndResize(0, 0, (double)primaryMonitorWidth * scale + 1, (double)primaryMonitorHeight * scale + 1);
            this.Show(false);
        }
    }

    #endregion

    private partial class BlurredBackdrop : CompositionBrushBackdrop
    {
        protected override CompositionBrush CreateBrush(Compositor compositor)
            => compositor.CreateHostBackdropBrush();
    }
}
