﻿using CommunityToolkit.Mvvm.Input;

namespace DesktopWidgets3.Views.Windows;

public sealed partial class OverlayWindow : WindowEx
{
    private readonly INavigationService _navigationService = DependencyExtensions.GetRequiredService<INavigationService>();
    private readonly IWidgetManagerService _widgetManagerService = DependencyExtensions.GetRequiredService<IWidgetManagerService>();

    public OverlayWindow()
    {
        InitializeComponent();

        Title = string.Empty;

        IsTitleBarVisible = IsMaximizable = IsMaximizable = IsResizable = false;

        IsAlwaysOnTop = true;

        SystemHelper.HideWindowIconFromTaskbar(this.GetWindowHandle());
    }

    public void CenterTopOnMonitor()
    {
        var monitorInfo = DisplayMonitor.GetMonitorInfo(this);
        var monitorWidth = monitorInfo.RectMonitor.Width;
        if (monitorWidth != null)
        {
            var windowWidth = AppWindow.Size.Width;
            this.Move((int)(monitorWidth - windowWidth) / 2, 8);
        }
    }

    #region Commands

    [RelayCommand]
    private async Task SaveAndExitEditModeAsync()
    {
        await _widgetManagerService.SaveAndExitEditMode();
    }

    [RelayCommand]
    private void NavigateSettingsPage()
    {
        _navigationService.NavigateTo(typeof(SettingsViewModel).FullName!);
        App.ShowMainWindow(true);
    }

    [RelayCommand]
    private void CancelChangesAndExitEditMode()
    {
        _widgetManagerService.CancelChangesAndExitEditMode();
    }

    #endregion
}
