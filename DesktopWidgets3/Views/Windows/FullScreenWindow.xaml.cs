using H.NotifyIcon;
using Windows.UI.Composition;

namespace DesktopWidgets3.Views.Windows;

public sealed partial class FullScreenWindow : WindowEx
{
    public FullScreenWindow()
    {
        InitializeComponent();

        Title = string.Empty;

        Content = null;

        IsTitleBarVisible = IsMaximizable = IsMaximizable = IsResizable = false;

        IsAlwaysOnTop = true;

        SystemBackdrop = new BlurredBackdrop();

        SystemHelper.HideWindowIconFromTaskbar(this.GetWindowHandle());
    }

    #region Backdrop

    private partial class BlurredBackdrop : CompositionBrushBackdrop
    {
        protected override CompositionBrush CreateBrush(Compositor compositor)
            => compositor.CreateHostBackdropBrush();
    }

    #endregion

    #region Hide & Show & Activate

    private bool activated = false;

    public void Hide()
    {
        this.Hide(true);
    }

    public void Show()
    {
        if (!activated)
        {
            Activate();
        }
        else
        {
            FullScreen();
            this.Show(true);
        }
    }

    public new void Activate()
    {
        FullScreen();
        base.Activate();
        activated = true;
    }

    private void FullScreen()
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
}
