﻿using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;

namespace DesktopWidgets3.ViewModels.Pages;

public partial class SettingsPageViewModel : ObservableRecipient, INavigationAware
{
    #region view properties

    public ObservableCollection<AppLanguageItem> AppLanguages = AppLanguageHelper.SupportedLanguages;

    public Visibility NonlogonTaskCardVisibility = RuntimeHelper.IsMSIX ? Visibility.Visible : Visibility.Collapsed;
    public Visibility LogonTaskExpanderVisibility = RuntimeHelper.IsMSIX ? Visibility.Collapsed : Visibility.Visible;

    [ObservableProperty]
    private int _languageIndex;

    [ObservableProperty]
    private bool _showRestartTipLanguage;

    [ObservableProperty]
    private bool _runStartup;

    [ObservableProperty]
    private bool _logonTask;

    [ObservableProperty]
    private bool _silentStart;

    [ObservableProperty]
    private bool _batterySaver;

    [ObservableProperty]
    private int _themeIndex;

    [ObservableProperty]
    private int _backdropTypeIndex;

    [ObservableProperty]
    private bool _enableMicrosoftWidgets;

    [ObservableProperty]
    private bool _showRestartTipMicrosoftWidgets;

    [ObservableProperty]
    private string _appDisplayName = ConstantHelper.AppDisplayName;

    [ObservableProperty]
    private string _version = $"v{InfoHelper.GetVersion()}";

    [ObservableProperty]
    private string _copyRight = $"{InfoHelper.GetCopyright()}";

    #endregion

    private readonly IAppSettingsService _appSettingsService;
    private readonly IBackdropSelectorService _backdropSelectorService;
    private readonly IThemeSelectorService _themeSelectorService;

    private bool _isInitialized;

    public SettingsPageViewModel(IAppSettingsService appSettingsService, IBackdropSelectorService backdropSelectorService, IThemeSelectorService themeSelectorService)
    {
        _appSettingsService = appSettingsService;
        _backdropSelectorService = backdropSelectorService;
        _themeSelectorService = themeSelectorService;

        InitializeSettings();
    }

    private void InitializeSettings()
    {
        SilentStart = _appSettingsService.SilentStart;
        BatterySaver = _appSettingsService.BatterySaver;
        ThemeIndex = (int)_themeSelectorService.Theme;
        BackdropTypeIndex = (int)_appSettingsService.BackdropType;
        EnableMicrosoftWidgets = _appSettingsService.EnableMicrosoftWidgets;

        _isInitialized = true;
    }

    #region INavigationAware

    public async void OnNavigatedTo(object parameter)
    {
        LanguageIndex = AppLanguageHelper.SupportedLanguages.IndexOf(AppLanguageHelper.PreferredLanguage);

        var logonTask = await StartupHelper.GetStartupAsync(logon: true);
        var startupEntry = await StartupHelper.GetStartupAsync();
        RunStartup = logonTask || startupEntry;
        LogonTask = logonTask;

        ShowRestartTipLanguage = false;
        ShowRestartTipMicrosoftWidgets = false;
    }

    public void OnNavigatedFrom()
    {

    }

    #endregion

    #region Commands

#pragma warning disable CA1822 // Mark members as static
    [RelayCommand]
    private void RestartApplication()
    {
        App.RestartApplication();
    }
#pragma warning restore CA1822 // Mark members as static

    [RelayCommand]
    private void CancelRestartLanguage()
    {
        ShowRestartTipLanguage = false;
    }

    [RelayCommand]
    private void CancelRestartMicrosoftWidgets()
    {
        ShowRestartTipMicrosoftWidgets = false;
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
                _appSettingsService.SetLanguageAsync(AppLanguageHelper.GetLanguageCode(value));
            }

            ShowRestartTipLanguage = true;
        }
    }

    partial void OnRunStartupChanged(bool value)
    {
        if (_isInitialized)
        {
            if (value)
            {
                _ = StartupHelper.SetStartupAsync(true, logon: LogonTask);
            }
            else
            {
                _ = StartupHelper.SetStartupAsync(false, logon: true);
                _ = StartupHelper.SetStartupAsync(false);
            }
        }
    }

    partial void OnLogonTaskChanged(bool value)
    {
        if (_isInitialized)
        {
            if (RunStartup)
            {
                _ = StartupHelper.SetStartupAsync(false, logon: !value);
                _ = StartupHelper.SetStartupAsync(true, logon: value);
            }
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

    partial void OnThemeIndexChanged(int value)
    {
        if (_isInitialized)
        {
            _themeSelectorService.SetThemeAsync((ElementTheme)value);
        }
    }

    partial void OnBackdropTypeIndexChanged(int value)
    {
        if (_isInitialized)
        {
            _backdropSelectorService.SetBackdropTypeAsync((BackdropType)value);
        }
    }

    partial void OnEnableMicrosoftWidgetsChanged(bool value)
    {
        if (_isInitialized)
        {
            _appSettingsService.SetEnableMicrosoftWidgetsAsync(value);

            ShowRestartTipMicrosoftWidgets = true;
        }
    }

    #endregion
}
