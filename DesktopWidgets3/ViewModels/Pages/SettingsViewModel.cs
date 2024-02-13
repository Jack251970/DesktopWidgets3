using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using DesktopWidgets3.Contracts.ViewModels;
using DesktopWidgets3.Helpers;

namespace DesktopWidgets3.ViewModels.Pages;

public partial class SettingsViewModel : ObservableRecipient, INavigationAware
{
    #region view properties

    [ObservableProperty]
    private int _themeIndex;
    [ObservableProperty]
    private bool _runStartup;
    [ObservableProperty]
    private bool _silentStart;
    [ObservableProperty]
    private bool _batterySaver;
    [ObservableProperty]
    private string _version = $"v{InfoHelper.GetVersion()}";

    #endregion

    private readonly IAppSettingsService _appSettingsService;
    private readonly IThemeSelectorService _themeSelectorService;
    private readonly IWidgetManagerService _widgetManagerService;

    private bool _isInitialized;

    public SettingsViewModel(IAppSettingsService appSettingsService, IThemeSelectorService themeSelectorService, IWidgetManagerService widgetManagerService)
    {
        _appSettingsService = appSettingsService;
        _themeSelectorService = themeSelectorService;
        _widgetManagerService = widgetManagerService;

        InitializeSettings();
    }

    private async void InitializeSettings()
    {
        ThemeIndex = (int)_themeSelectorService.Theme;
        RunStartup = await StartupHelper.GetStartup();
        SilentStart = _appSettingsService.SilentStart;
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
        }
    }

    partial void OnRunStartupChanged(bool value)
    {
        if (_isInitialized)
        {
#if !DEBUG
            _ = StartupHelper.SetStartupAsync(value);
#endif
        }
    }

    partial void OnSilentStartChanged(bool value)
    {
        if (_isInitialized)
        {
            _appSettingsService.SetSilentStartAsync(value);
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
