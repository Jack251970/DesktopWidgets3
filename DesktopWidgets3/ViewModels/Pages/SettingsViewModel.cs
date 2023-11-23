using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    private bool _showSeconds;
    [ObservableProperty]
    private bool _strictMode;
    [ObservableProperty]
    private int _breakInterval;
    [ObservableProperty]
    private bool _forbidQuit;
    [ObservableProperty]
#if DEBUG
    private string _version = $"v{AssemblyHelper.GetVersion()} - DEBUG";
#else
    private string _version = $"v{AssemblyHelper.GetVersion()}";
#endif
    [ObservableProperty]
    private bool _lockOptionsEnabled = true;

    private readonly IAppSettingsService _appSettingsService;
    private readonly INavigationService _navigationService;
    private readonly IThemeSelectorService _themeSelectorService;
    private readonly IWidgetManagerService _widgetManagerService;

    private bool _isInitialized;

    public SettingsViewModel(IAppSettingsService appSettingsService, INavigationService navigationService, IThemeSelectorService themeSelectorService, IWidgetManagerService widgetManagerService)
    {
        _navigationService = navigationService;
        _appSettingsService = appSettingsService;
        _themeSelectorService = themeSelectorService;
        _widgetManagerService = widgetManagerService;

        InitializeViewModel();
    }

    private async void InitializeViewModel()
    {
        ThemeIndex = (int)_themeSelectorService.Theme;
        RunStartup = await StartupHelper.GetStartup();
        BatterySaver = await _appSettingsService.GetBatterySaverAsync();
        ShowSeconds = await _appSettingsService.GetShowSecondsAsync();
        StrictMode = await _appSettingsService.GetStrictModeAsync();
        BreakInterval = await _appSettingsService.GetBreakIntervalAsync();
        ForbidQuit = await _appSettingsService.GetForbidQuitAsync();

        _isInitialized = true;
    }

    public void OnNavigatedTo(object parameter)
    {
        LockOptionsEnabled = !_appSettingsService.IsLocking;
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

    partial void OnShowSecondsChanged(bool value)
    {
        if (_isInitialized)
        {
            _appSettingsService.SetShowSecondsAsync(value);
        }
    }

    partial void OnStrictModeChanged(bool value)
    {
        if (_isInitialized)
        {
            _appSettingsService.SetStrictModeAsync(value);
        }
    }

    partial void OnForbidQuitChanged(bool value)
    {
        if (_isInitialized)
        {
            _appSettingsService.SetForbidQuitAsync(value);
        } 
    }

    partial void OnBreakIntervalChanged(int value)
    {
        if (_isInitialized)
        {
            _appSettingsService.SetBreakIntervalAsync(value);
        }  
    }
}
