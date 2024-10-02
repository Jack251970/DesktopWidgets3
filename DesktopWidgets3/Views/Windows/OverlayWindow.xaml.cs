using CommunityToolkit.Mvvm.Input;
using H.NotifyIcon;

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

    #region Hide & Show & Activate

    private bool activated = false;

    public void Hide()
    {
        this.Hide(true);
    }

    public void Show()
    {
        if (!activated)
        {
            Activate();
        }
        else
        {
            CenterTopOnMonitor();
            this.Show(true);
        }
    }

    public new void Activate()
    {
        CenterTopOnMonitor();
        base.Activate();
        activated = true;
    }

    private void CenterTopOnMonitor()
    {
        var monitorInfo = DisplayMonitor.GetMonitorInfo(this);
        var monitorWidth = monitorInfo.RectMonitor.Width;
        if (monitorWidth != null)
        {
            var windowWidth = AppWindow.Size.Width;
            this.Move((int)(monitorWidth - windowWidth) / 2, 8);
        }
    }

    #endregion

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
