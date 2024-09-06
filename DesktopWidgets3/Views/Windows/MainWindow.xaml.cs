using H.NotifyIcon;

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

using Windows.UI.ViewManagement;

namespace DesktopWidgets3.Views.Windows;

public sealed partial class MainWindow : WindowEx
{
    private readonly DispatcherQueue dispatcherQueue;

    private readonly UISettings settings;

    #region ui elements

    public UIElement? TitleBar { get; set; }

    public UIElement? TitleBarText { get; set; }

    public new bool Visible { get; set; } = true;

    #endregion

    #region manager & handle

    public WindowManager WindowManager => _manager;
    public IntPtr WindowHandle => _handle;

    private readonly WindowManager _manager;
    private readonly IntPtr _handle;

    #endregion

    public MainWindow()
    {
        InitializeComponent();

        _manager = WindowManager.Get(this);
        _handle = this.GetWindowHandle();

        AppWindow.SetIcon("/Assets/WindowIcon.ico");
        Content = null;
        Title = "AppDisplayName".GetLocalized();

        // Theme change code picked from https://github.com/microsoft/WinUI-Gallery/pull/1239
        dispatcherQueue = ThreadExtensions.MainDispatcherQueue!;
        settings = new UISettings();
        settings.ColorValuesChanged += Settings_ColorValuesChanged; // cannot use FrameworkElement.ActualThemeChanged event

        Closed += WindowEx_Closed;
    }

    #region Hide & Show

    public void Hide()
    {
        this.Hide(true);
        Visible = false;
    }

    public void Show()
    {
        this.Show(true);
        Visible = true;
    }

    #endregion

    // This handles updating the caption button colors correctly when windows system theme is changed while the app is open
    private void Settings_ColorValuesChanged(UISettings sender, object args)
    {
        // This calls comes off-thread, hence we will need to dispatch it to current app's thread
        dispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () => TitleBarHelper.ApplySystemThemeToCaptionButtons(null, null, TitleBarText));
    }

    // this enables the app to continue running in background after clicking close button
    private async void WindowEx_Closed(object sender, WindowEventArgs args)
    {
        if (App.CanCloseWindow)
        {
            ApplicationLifecycleExtensions.MainWindow_Closed_Widgets_Closing?.Invoke(this, args);
            await App.GetService<IWidgetManagerService>().DisableAllWidgets();
            await WindowsExtensions.CloseAllWindows();
            ApplicationLifecycleExtensions.MainWindow_Closed_Widgets_Closed?.Invoke(this, args);
            LogExtensions.LogInformation("Exit current application.");
            Application.Current.Exit();
        }
        else
        {
            args.Handled = true;
            ApplicationLifecycleExtensions.MainWindow_Hiding?.Invoke(this, args);
            Hide();
            Visible = false;
            ApplicationLifecycleExtensions.MainWindow_Hided?.Invoke(this, args);
        }
    }
}
