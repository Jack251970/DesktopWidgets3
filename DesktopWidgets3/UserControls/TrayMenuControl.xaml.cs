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

    private readonly IWidgetManagerService _widgetManagerService = DependencyExtensions.GetRequiredService<IWidgetManagerService>();

    public TrayMenuControl()
    {
        InitializeComponent();
    }

    #region Commands

    [RelayCommand]
    private void ShowWindow()
    {
        App.ShowMainWindow(false);
    }

    [RelayCommand]
    private async Task ExitAppAsync()
    {
        await _widgetManagerService.CheckEditModeAsync();
        DisposeTrayIconControl();
        App.CanCloseWindow = true;
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
