using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;

namespace DesktopWidgets3.Views.Windows;

public sealed partial class OverlayWindow : WindowEx
{
    #region position & size

    public PointInt32 Position
    {
        get => AppWindow.Position;
        set => WindowExtensions.Move(this, value.X, value.Y);
    }

    public SizeInt32 Size
    {
        get => new((int)(AppWindow.Size.Width * 96f / WindowExtensions.GetDpiForWindow(this)), (int)(AppWindow.Size.Height * 96f / WindowExtensions.GetDpiForWindow(this)));
        set => WindowExtensions.SetWindowSize(this, value.Width, value.Height);
    }

    #endregion

    public OverlayWindow()
    {
        InitializeComponent();

        Content = new Frame();
        Title = string.Empty;

        Initialize();
    }

    private void Initialize()
    {
        // Hide title bar, set window unresizable
        IsTitleBarVisible = IsMaximizable = IsMaximizable = IsResizable = false;

        // Set always on top
        IsAlwaysOnTop = true;

        // Hide window icon from taskbar
        SystemHelper.HideWindowIconFromTaskbar(this.GetWindowHandle());
    }
}
