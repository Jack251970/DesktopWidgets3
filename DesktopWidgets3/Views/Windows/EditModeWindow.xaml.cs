using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Views.Windows;

public sealed partial class EditModeWindow : WindowEx
{
    private readonly INavigationService _navigationService = DependencyExtensions.GetRequiredService<INavigationService>();
    private readonly IWidgetManagerService _widgetManagerService = DependencyExtensions.GetRequiredService<IWidgetManagerService>();

    public EditModeWindow()
    {
        InitializeComponent();

        Title = string.Empty;

        Activated += EditModeWindow_Activated;
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
        {
            WindowExtensions.Move(this, -10000, -10000);
            Activate();
        });
    }

    private void Content_Loaded(object sender, RoutedEventArgs e)
    {
        // Set the window size to the content desired size
        if (Content is FrameworkElement f)
        {
            if (f.DesiredSize.Width > 0)
            {
                Width = f.DesiredSize.Width;
            }
                
            if (f.DesiredSize.Height > 0)
            {
                Height = f.DesiredSize.Height;
            }
        }
    }

    private void EditModeWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        Activated -= EditModeWindow_Activated;
        this.Hide(); // Hides at the first time
        var hwnd = this.GetWindowHandle();
        HwndExtensions.SetWindowStyle(hwnd, WindowStyle.PopupWindow); // Set the window style to PopupWindow
        if (Content is not FrameworkElement content || content.IsLoaded)
        {
            Content_Loaded(this, new RoutedEventArgs());
        }
        else
        {
            content.Loaded += Content_Loaded;
        }
    }

    /// <summary>
    /// Show the window on the center top of the screen.
    /// </summary>
    public void Show()
    {
        var monitorInfo = DisplayMonitor.GetMonitorInfo(this);
        var monitorWidth = monitorInfo.RectMonitor.Width;
        if (monitorWidth != null)
        {
            var windowWidth = AppWindow.Size.Width;
            this.Move((int)(monitorWidth - windowWidth) / 2, 8);
            WindowExtensions.Show(this);
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
        _navigationService.NavigateTo(typeof(SettingsPageViewModel).FullName!);
        App.MainWindow.Show();
        App.MainWindow.BringToFront();
    }

    [RelayCommand]
    private void CancelChangesAndExitEditMode()
    {
        _widgetManagerService.CancelChangesAndExitEditMode();
    }

    #endregion
}
