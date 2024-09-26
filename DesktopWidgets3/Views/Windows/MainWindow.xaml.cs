using H.NotifyIcon;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using Windows.UI.ViewManagement;

namespace DesktopWidgets3.Views.Windows;

public sealed partial class MainWindow : WindowEx
{
    private static string ClassName => typeof(MainWindow).Name;

    private readonly IWidgetManagerService _widgetManagerService = DependencyExtensions.GetRequiredService<IWidgetManagerService>();
    private readonly IWidgetResourceService _widgetResourceService = DependencyExtensions.GetRequiredService<IWidgetResourceService>();

    private readonly DispatcherQueue dispatcherQueue;

    private readonly UISettings settings;

    #region Position & Size

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

    #region UI Elements

    public UIElement? TitleBar { get; set; }

    public UIElement? TitleBarText { get; set; }

    public new bool Visible { get; set; } = false;

    #endregion

    #region Manager & Handle

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

        AppWindow.SetIcon(Constant.AppIconPath);
        Title = ConstantHelper.AppAppDisplayName;
        Content = null;

        // Theme change code picked from https://github.com/microsoft/WinUI-Gallery/pull/1239
        dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        settings = new UISettings();
        settings.ColorValuesChanged += Settings_ColorValuesChanged; // cannot use FrameworkElement.ActualThemeChanged event

        Closed += WindowEx_Closed;
    }

    public void CenterOnRectWork()
    {
        var monitorInfo = DisplayMonitor.GetMonitorInfo(this);
        var rectWorkWidth = monitorInfo.RectWork.Width;
        var rectWorkHeight = monitorInfo.RectWork.Height;
        if (rectWorkWidth != null && rectWorkHeight != null)
        {
            var windowWidth = AppWindow.Size.Width;
            var windowHeight = AppWindow.Size.Height;
            Position = new PointInt32((int)((rectWorkWidth - windowWidth) / 2), (int)((rectWorkHeight - windowHeight) / 2));
        }
    }

    #region Hide & Show & Activate

    private bool activated = false;

    public void Hide()
    {
        this.Hide(true);
        Visible = false;
    }

    public void Show()
    {
        if (!activated)
        {
            Activate();
        }
        else
        {
            this.Show(true);
        }
        Visible = true;
    }

    public new void Activate()
    {
        base.Activate();
        activated = true;
    }

    #endregion

    #region Events

    // This handles updating the caption button colors correctly when windows system theme is changed while the app is open
    private void Settings_ColorValuesChanged(UISettings sender, object args)
    {
        // This calls comes off-thread, hence we will need to dispatch it to current app's thread
        dispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () => TitleBarHelper.ApplySystemThemeToCaptionButtons(this, AppWindow.TitleBar, TitleBarText));
    }

    // this enables the app to continue running in background after clicking close button
    private async void WindowEx_Closed(object sender, WindowEventArgs args)
    {
        if (App.CanCloseWindow)
        {
            App.FullScreenWindow.Close();
            await _widgetManagerService.CloseAllWidgetsAsync();
            await WindowsExtensions.CloseAllWindowsAsync();
            await _widgetResourceService.DisposeWidgetsAsync();
            WidgetsLoader.DisposeExtensionAssemblies();
            LogExtensions.LogInformation(ClassName, "Exit current application.");
            Application.Current.Exit();
        }
        else
        {
            args.Handled = true;
            Hide();
            Visible = false;
        }
    }

    #endregion
}
