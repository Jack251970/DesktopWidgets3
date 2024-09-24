using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.UserControls;

#pragma warning disable CA1822 // Mark members as static

[ObservableObject]
public sealed partial class TrayMenuControl : UserControl
{
    [ObservableProperty]
    private string _appDisplayName = ConstantHelper.AppAppDisplayName;

    public TrayMenuControl()
    {
        InitializeComponent();
    }

    [RelayCommand]
    private void ShowWindow()
    {
        App.ShowMainWindow(false);
    }

    [RelayCommand]
    private async Task ExitApp()
    {
        try
        {
            TrayIconControl.Dispose();
        }
        catch {}
        App.CanCloseWindow = true;
        await WindowsExtensions.CloseWindowAsync(App.MainWindow);
    }
}
