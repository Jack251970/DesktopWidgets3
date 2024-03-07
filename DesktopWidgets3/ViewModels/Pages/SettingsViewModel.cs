using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;

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
    private bool _multiThread;

    [ObservableProperty]
    private string _version = $"v{InfoHelper.GetVersion()}";

    #endregion

    private readonly IAppSettingsService _appSettingsService;
    private readonly IThemeSelectorService _themeSelectorService;

    private bool _isInitialized;

    public SettingsViewModel(IAppSettingsService appSettingsService, IThemeSelectorService themeSelectorService)
    {
        _appSettingsService = appSettingsService;
        _themeSelectorService = themeSelectorService;

        InitializeSettings();
    }

    private async void InitializeSettings()
    {
        ThemeIndex = (int)_themeSelectorService.Theme;
        RunStartup = await StartupHelper.GetStartup();
        SilentStart = _appSettingsService.SilentStart;
        BatterySaver = _appSettingsService.BatterySaver;
        MultiThread = _appSettingsService.MultiThread;

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

    partial void OnMultiThreadChanged(bool value)
    {
        if (_isInitialized)
        {
            _appSettingsService.SetMultiThreadAsync(value);
        }
    }
}
