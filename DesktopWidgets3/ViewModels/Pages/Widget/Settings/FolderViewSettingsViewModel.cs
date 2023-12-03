using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Contracts.ViewModels;
using DesktopWidgets3.Models.Widget;

namespace DesktopWidgets3.ViewModels.Pages.Widget.Settings;

public partial class FolderViewSettingsViewModel : ObservableRecipient, INavigationAware
{
    [ObservableProperty]
    private string _folderPath = $"C:\\";

    [ObservableProperty]
    private bool _showIconOverlay = true;

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
