using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;

namespace DesktopWidgets3.UserControls;

[ObservableObject]
public sealed partial class TrayMenuControl : UserControl
{
    [ObservableProperty]
    private string _trayIconToolTip = string.Empty;

    private readonly IWidgetManagerService _widgetManagerService = DependencyExtensions.GetRequiredService<IWidgetManagerService>();

    public TrayMenuControl()
    {
        InitializeComponent();
    }

    #region Commands

#pragma warning disable CA1822 // Mark members as static
    [RelayCommand]
    private void ShowWindow()
    {
        App.MainWindow.Show();
        App.MainWindow.BringToFront();
    }
#pragma warning restore CA1822 // Mark members as static

    [RelayCommand]
    private async Task ExitAppAsync()
    {
        await _widgetManagerService.CheckEditModeAsync();
        DisposeTrayIconControl();
#if TRAY_ICON
        App.CanCloseWindow = true;
#endif
        App.MainWindow.Close();
    }

    private void DisposeTrayIconControl()
    {
        try
        {
            TrayIconControl.Dispose();
        }
        catch { }
    }

    #endregion
}
