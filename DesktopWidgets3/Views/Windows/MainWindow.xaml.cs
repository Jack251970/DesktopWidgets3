using H.NotifyIcon;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Helpers;
using Windows.UI.ViewManagement;

namespace DesktopWidgets3.Views.Windows;

public sealed partial class MainWindow : WindowEx
{
    private readonly IAppSettingsService _appSettingsService = App.GetService<IAppSettingsService>();

    private readonly ITimersService _timersService = App.GetService<ITimersService>();

    private readonly DispatcherQueue dispatcherQueue;

    private readonly UISettings settings;

    public MainWindow()
    {
        InitializeComponent();

        // TODO: You need to add Post-build event to make sure icon exists.
        // mkdir $(TargetDir)Assets
        // copy $(ProjectDir)Assets\WindowIcon.ico $(TargetDir)Assets\WindowIcon.ico
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/WindowIcon.ico"));
        Content = null;
        Title = "AppDisplayName".GetLocalized();

        // Theme change code picked from https://github.com/microsoft/WinUI-Gallery/pull/1239
        dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        settings = new UISettings();
        settings.ColorValuesChanged += Settings_ColorValuesChanged; // cannot use FrameworkElement.ActualThemeChanged event
    }

    // this handles updating the caption button colors correctly when indows system theme is changed while the app is open
    private void Settings_ColorValuesChanged(UISettings sender, object args)
    {
        // This calls comes off-thread, hence we will need to dispatch it to current app's thread
        dispatcherQueue.TryEnqueue(TitleBarHelper.ApplySystemThemeToCaptionButtons);
    }

    // this enables the app to continue running in background after clicking close button and the battery saver feature
    private void WindowEx_Closed(object sender, WindowEventArgs args)
    {
        if (App.CheckCanCloseWindow())
        {
            if (_appSettingsService.ForbidQuit)
            {
                args.Handled = true;
                SystemHelper.MessageBox("TrayMenu_ExitApp_ForbidQuit".GetLocalized(), "MessageBox_Title_Warning".GetLocalized());
            }
            else if (_appSettingsService.IsLocking)
            {
                args.Handled = true;
                SystemHelper.MessageBox("TrayMenu_ExitApp_IsLocking".GetLocalized(), "MessageBox_Title_Warning".GetLocalized());
            }
            else
            {
                App.CloseClockWindow();
                App.CloseCPUWindow();
                Application.Current.Exit();
            }
        }
        else
        {
            args.Handled = true;
            this.Hide(true);
            _timersService.StopUpdateTimeTimer();
        }
    }

    // this enables the battery saver feature
    private void WindowEx_VisibilityChanged(object sender, WindowVisibilityChangedEventArgs args)
    {
        var visible = args.Visible;
        if (visible)
        {
            _timersService.StartUpdateTimeTimer();
        }
        else
        {
            _timersService.StopUpdateTimeTimer();
        }
    }
}
