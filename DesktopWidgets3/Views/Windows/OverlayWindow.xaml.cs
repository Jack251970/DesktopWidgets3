using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;

namespace DesktopWidgets3.Views.Windows;

public sealed partial class OverlayWindow : WindowEx
{
    #region position & size

    public PointInt32 Position
    {
        get => AppWindow.Position;
        set => this.Move(value.X, value.Y);
    }

    public SizeInt32 Size
    {
        get => new((int)(AppWindow.Size.Width * 96f / this.GetDpiForWindow()), (int)(AppWindow.Size.Height * 96f / this.GetDpiForWindow()));
        set => this.SetWindowSize(value.Width, value.Height);
    }

    #endregion

    private const int EditModeOverlayWindowXamlWidth = 136;  // 40 * 3 + 4 * 2 * 2
    private const int EditModeOverlayWindowXamlHeight = 48;  // 40 + 4 * 2

    public OverlayWindow()
    {
        InitializeComponent();

        Content = new Frame();
        Title = string.Empty;
    }

    public void Initialize()
    {
        // Hide title bar, set window unresizable
        IsTitleBarVisible = IsMaximizable = IsMaximizable = IsResizable = false;

        // Set always on top
        IsAlwaysOnTop = true;

        // Hide window icon from taskbar
        SystemHelper.HideWindowIconFromTaskbar(this.GetWindowHandle());

        // set window size according to xaml, rember larger than 136 x 39
        Size = new SizeInt32(EditModeOverlayWindowXamlWidth, EditModeOverlayWindowXamlHeight);
    }

    public void CenterTopOnMonitor()
    {
        var monitorInfo = DisplayMonitor.GetMonitorInfo(this);
        var monitorWidth = monitorInfo.RectMonitor.Width;
        var windowWidth = AppWindow.Size.Width;
        Position = new PointInt32((int)((monitorWidth! - windowWidth) / 2), 0);
    }
}
