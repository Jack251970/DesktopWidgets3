using Microsoft.UI.Dispatching;

using Windows.UI.ViewManagement;
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

    private readonly UISettings settings;

    public OverlayWindow()
    {
        InitializeComponent();

        Content = null;
        Title = string.Empty;

        settings = new UISettings();
        settings.ColorValuesChanged += Settings_ColorValuesChanged; // cannot use FrameworkElement.ActualThemeChanged event

        Initialize();
    }

    // This handles updating the caption button colors correctly when indows system theme is changed while the app is open
    private void Settings_ColorValuesChanged(UISettings sender, object args)
    {
        // This calls comes off-thread, hence we will need to dispatch it to current app's thread
        DispatcherQueue.GetForCurrentThread().TryEnqueue(DispatcherQueuePriority.High, () => TitleBarHelper.ApplySystemThemeToCaptionButtons(this, null));
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
