using Microsoft.UI.Dispatching;
using DesktopWidgets3.Helpers;
using Windows.UI.ViewManagement;

namespace DesktopWidgets3.Views.Windows;

public sealed partial class OverlayWindow : WindowEx
{
    private readonly DispatcherQueue dispatcherQueue;

    private readonly UISettings settings;

    public OverlayWindow()
    {
        InitializeComponent();

        Content = null;
        Title = string.Empty;

        // Theme change code picked from https://github.com/microsoft/WinUI-Gallery/pull/1239
        dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        settings = new UISettings();
        settings.ColorValuesChanged += Settings_ColorValuesChanged; // cannot use FrameworkElement.ActualThemeChanged event

        // Hide title bar, set window unresizable and always on top
        IsTitleBarVisible = IsMaximizable = IsMaximizable = IsResizable = false;
        IsAlwaysOnTop = true;
    }

    // this handles updating the caption button colors correctly when indows system theme is changed while the app is open
    private void Settings_ColorValuesChanged(UISettings sender, object args)
    {
        // This calls comes off-thread, hence we will need to dispatch it to current app's thread
        dispatcherQueue.TryEnqueue(TitleBarHelper.ApplySystemThemeToCaptionButtons);
    }
}
