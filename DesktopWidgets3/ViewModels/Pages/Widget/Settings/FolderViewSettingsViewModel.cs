using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Contracts.ViewModels;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.Models.Widget;
using DesktopWidgets3.ViewModels.Commands;

namespace DesktopWidgets3.ViewModels.Pages.Widget.Settings;

public partial class FolderViewSettingsViewModel : ObservableRecipient, INavigationAware
{
    [ObservableProperty]
    private string _folderPath = $"C:\\";

    [ObservableProperty]
    private bool _showIconOverlay = true;
    
    public ClickCommand SelectFolderPathCommand { get; set; }

    private readonly INavigationService _navigationService;
    private readonly IWidgetManagerService _widgetManagerService;

    private readonly WidgetType widgetType = WidgetType.FolderView;
    private int indexTag = -1;

    private BaseWidgetSettings? _widgetSettings;

    private bool _isInitialized;

    public FolderViewSettingsViewModel(INavigationService navigationService, IWidgetManagerService widgetManagerService)
    {
        _navigationService = navigationService;
        _widgetManagerService = widgetManagerService;

        SelectFolderPathCommand = new ClickCommand(SelectFoldePath);
    }

    private async void SelectFoldePath()
    {
        if (_isInitialized)
        {
            var newPath = await PickSingleFolderDialog();
            if (!string.IsNullOrEmpty(newPath))
            {
                var settings = _widgetSettings as FolderViewWidgetSettings;
                settings!.FolderPath = FolderPath = newPath;
                await _widgetManagerService.UpdateWidgetSettings(widgetType, indexTag, settings);
            }
        }
    }

    private async Task<string> PickSingleFolderDialog()
    {
        // This function was changed to use the shell32 API to open folder dialog
        // as the old one (PickSingleFolderAsync) can't work when the process is elevated
        // TODO: go back PickSingleFolderAsync when it's fixed
        var hwnd = WindowExtensions.GetWindowHandle(App.MainWindow!);
        var r = await Task.FromResult<string>(ShellGetFolder.GetFolderDialog(hwnd));
        return r;
    }

    public void InitializeSettings()
    {
        var settings = _widgetSettings as FolderViewWidgetSettings;
        FolderPath = settings!.FolderPath;
        ShowIconOverlay = settings.ShowIconOverlay;
    }

    public async void OnNavigatedTo(object parameter)
    {
        if (parameter is Dictionary<string, object> param)
        {
            if (param.TryGetValue("WidgetType", out var widgetTypeObj) && (WidgetType)widgetTypeObj == widgetType && param.TryGetValue("IndexTag", out var indexTagObj))
            {
                indexTag = (int)indexTagObj;
                _widgetSettings = await _widgetManagerService.GetWidgetSettings(widgetType, indexTag);
                if (_widgetSettings != null)
                {
                    InitializeSettings();
                    _isInitialized = true;
                }
            }
        }

        if (!_isInitialized)
        {
            var dashboardPageKey = typeof(DashboardViewModel).FullName!;
            _navigationService.NavigateTo(dashboardPageKey);
        }
    }

    public void OnNavigatedFrom()
    {
        
    }

    partial void OnShowIconOverlayChanged(bool value)
    {
        if (_isInitialized)
        {
            var settings = _widgetSettings as FolderViewWidgetSettings;
            settings!.ShowIconOverlay = value;
            _widgetManagerService.UpdateWidgetSettings(widgetType, indexTag, settings);
        }
    }
}
