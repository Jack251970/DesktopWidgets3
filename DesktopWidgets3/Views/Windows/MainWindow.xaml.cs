using H.NotifyIcon;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using DesktopWidgets3.Helpers;
using Windows.UI.ViewManagement;

namespace DesktopWidgets3.Views.Windows;

public sealed partial class MainWindow : WindowEx
{
    private readonly DispatcherQueue dispatcherQueue;

    private readonly UISettings settings;

    public MainWindow()
    {
        InitializeComponent();

        AppWindow.SetIcon("/Assets/WindowIcon.ico");
        Content = null;
        Title = "AppDisplayName".GetLocalized();

        // Theme change code picked from https://github.com/microsoft/WinUI-Gallery/pull/1239
        dispatcherQueue = UIThreadExtensions.DispatcherQueue!;
        settings = new UISettings();
        settings.ColorValuesChanged += Settings_ColorValuesChanged; // cannot use FrameworkElement.ActualThemeChanged event

        Closed += (s, a) => WindowEx_Closed(a);
    }

    // this handles updating the caption button colors correctly when windows system theme is changed while the app is open
    private void Settings_ColorValuesChanged(UISettings sender, object args)
    {
        // This calls comes off-thread, hence we will need to dispatch it to current app's thread
        dispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, TitleBarHelper.ApplySystemThemeToCaptionButtons);
    }

    // this enables the app to continue running in background after clicking close button
    private void WindowEx_Closed(WindowEventArgs args)
    {
        if (App.CanCloseWindow)
        {
            ApplicationExtensions.MainWindow_Closed_Widgets_Closing?.Invoke(this, args);
            App.GetService<IWidgetManagerService>().DisableAllWidgets();
            ApplicationExtensions.MainWindow_Closed_Widgets_Closed?.Invoke(this, args);
            Application.Current.Exit();
        }
        else
        {
            args.Handled = true;
            ApplicationExtensions.MainWindow_Hiding?.Invoke(this, args);
            this.Hide(true);
            ApplicationExtensions.MainWindow_Hided?.Invoke(this, args);
        }
    }
}
