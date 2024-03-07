using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace DesktopWidgets3.Views.Controls;

#pragma warning disable CA1822 // Mark members as static

public sealed partial class TrayMenuControl : UserControl
{
    public TrayMenuControl()
    {
        InitializeComponent();
    }

    private void ShowWindow(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        App.ShowMainWindow(false);
        ApplicationLifecycleExtensions.MainWindow_Closing?.Invoke(App.MainWindow, WindowEventArgs.FromAbi(App.MainWindow.GetWindowHandle()));
    }

    private void ExitApp(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        TrayIcon.Dispose();
        App.CanCloseWindow = true;
        UIElementExtensions.CloseWindow(App.MainWindow);
    }
}