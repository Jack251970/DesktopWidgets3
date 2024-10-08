using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;

namespace DesktopWidgets3.ViewModels.Pages;

public partial class SettingsViewModel : ObservableRecipient, INavigationAware
{
    #region view properties

    public ObservableCollection<AppLanguageItem> AppLanguages = AppLanguageHelper.SupportedLanguages;

    [ObservableProperty]
    private int _languageIndex;

    [ObservableProperty]
    private bool _showRestartTip;

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
    private string _appDisplayName = ConstantHelper.AppAppDisplayName;

    [ObservableProperty]
    private string _version = $"v{InfoHelper.GetVersion()}";

    [ObservableProperty]
    private string _copyRight = $"{InfoHelper.GetCopyright()}";

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

    private void InitializeSettings()
    {
        ThemeIndex = (int)_themeSelectorService.Theme;
        SilentStart = _appSettingsService.SilentStart;
        BatterySaver = _appSettingsService.BatterySaver;
        MultiThread = _appSettingsService.MultiThread;

        _isInitialized = true;
    }

    #region INavigationAware

    public async void OnNavigatedTo(object parameter)
    {
        LanguageIndex = AppLanguageHelper.SupportedLanguages.IndexOf(AppLanguageHelper.PreferredLanguage);
        RunStartup = await StartupHelper.GetStartup(Constant.StartupTaskId, Constant.StartupRegistryKey);

        ShowRestartTip = false;
    }

    public void OnNavigatedFrom()
    {

    }

    #endregion

    #region Commands

    [RelayCommand]
    private void RestartApplication()
    {
        App.RestartApplication();
    }

    [RelayCommand]
    private void CancelRestart()
    {
        ShowRestartTip = false;
    }

    #endregion

    #region Property Events

    partial void OnLanguageIndexChanging(int value)
    {
        if (_isInitialized)
        {
            if (RuntimeHelper.IsMSIX)
            {
                // No need to store the preference in packaged app - it is already stored by the app
                AppLanguageHelper.TryChange(value);
            }
            else
            {
                // No need to set PrimaryLanguageOverride in unpackaged app - it will be set by the app in the next launch
                _appSettingsService.SaveLanguageInSettingsAsync(AppLanguageHelper.GetLanguageCode(value));
            }

            ShowRestartTip = true;
        }
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
            _ = StartupHelper.SetStartupAsync(Constant.StartupTaskId, Constant.StartupRegistryKey, value);
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

    #endregion
}
