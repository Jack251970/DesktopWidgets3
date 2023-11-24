using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Contracts.ViewModels;
using DesktopWidgets3.Helpers;

namespace DesktopWidgets3.ViewModels.Pages;

public partial class SettingsViewModel : ObservableRecipient, INavigationAware
{
    [ObservableProperty]
    private int _themeIndex;
    [ObservableProperty]
    private bool _batterySaver;
    [ObservableProperty]
    private bool _runStartup;
    [ObservableProperty]
#if DEBUG
    private string _version = $"v{AssemblyHelper.GetVersion()} - DEBUG";
#else
    private string _version = $"v{AssemblyHelper.GetVersion()}";
#endif
    [ObservableProperty]
    private bool _lockOptionsEnabled = true;

    private readonly IAppSettingsService _appSettingsService;
    private readonly IThemeSelectorService _themeSelectorService;
    private readonly IWidgetManagerService _widgetManagerService;

    private bool _isInitialized;

    public SettingsViewModel(IAppSettingsService appSettingsService, IThemeSelectorService themeSelectorService, IWidgetManagerService widgetManagerService)
    {
        _appSettingsService = appSettingsService;
        _themeSelectorService = themeSelectorService;
        _widgetManagerService = widgetManagerService;

        InitializeViewModel();
    }

    private async void InitializeViewModel()
    {
        ThemeIndex = (int)_themeSelectorService.Theme;
        RunStartup = await StartupHelper.GetStartup();
        BatterySaver = _appSettingsService.BatterySaver;

        _isInitialized = true;
    }

    public void OnNavigatedTo(object parameter)
    {

    }

    public void OnNavigatedFrom()
    {
        
    }

    partial void OnThemeIndexChanged(int value)
    {
        if (_isInitialized)
        {
            _themeSelectorService.SetThemeAsync((ElementTheme)value);
            _widgetManagerService.SetThemeAsync();
        }
    }

    partial void OnRunStartupChanged(bool value)
    {
        if (_isInitialized)
        {
            _ = StartupHelper.SetStartupAsync(value);
        }
    }

    partial void OnBatterySaverChanged(bool value)
    {
        if (_isInitialized)
        {
            _appSettingsService.SetBatterySaverAsync(value);
        }
    }
}
