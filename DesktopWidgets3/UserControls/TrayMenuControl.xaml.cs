using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.UserControls;

#pragma warning disable CA1822 // Mark members as static

public sealed partial class TrayMenuControl : UserControl
{
    public TrayMenuControl()
    {
        InitializeComponent();
#if DEBUG
        TrayIconControl.ToolTipText += " (Debug)";
#endif
    }

    [RelayCommand]
    private void ShowWindow()
    {
        App.ShowMainWindow(false);
        ApplicationLifecycleExtensions.MainWindow_Closing?.Invoke(App.MainWindow, null!);
    }

    [RelayCommand]
    private async Task ExitApp()
    {
        try
        {
            TrayIconControl.Dispose();
        }
        catch
        {
            
        }
        App.CanCloseWindow = true;
        await WindowsExtensions.CloseWindow(App.MainWindow);
    }
}