﻿using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Services;

internal class ThemeSelectorService(ILocalSettingsService localSettingsService, IOptions<LocalSettingsKeys> localSettingsKeys) : IThemeSelectorService
{
    private ElementTheme theme = ElementTheme.Default;
    public ElementTheme Theme {
        get => theme;
        set
        {
            if (theme != value)
            {
                theme = value;
                if (_isInitialized)
                {
                    OnThemeChanged?.Invoke(value);
                }
            }
        } 
    }

    public event Action<ElementTheme>? OnThemeChanged;

    private readonly ILocalSettingsService _localSettingsService = localSettingsService;
    private readonly LocalSettingsKeys _localSettingsKeys = localSettingsKeys.Value;

    private string SettingsKey => _localSettingsKeys.ThemeKey;

    private bool _isInitialized;

    public async Task InitializeAsync()
    {
        if (!_isInitialized)
        {
            Theme = await LoadThemeFromSettingsAsync();
            await Task.CompletedTask;

            _isInitialized = true;
        }
    }

    public async Task SetRequestedThemeAsync(Window window)
    {
        ThemeHelper.SetRequestedThemeAsync(window, Theme);

        await Task.CompletedTask;
    }

    public async Task SetThemeAsync(ElementTheme theme)
    {
        Theme = theme;

        await SetRequestedThemeAsync(App.MainWindow);

        await WindowsExtensions.GetAllWindows().EnqueueOrInvokeAsync(SetRequestedThemeAsync, Microsoft.UI.Dispatching.DispatcherQueuePriority.High);

        await SaveThemeInSettingsAsync(Theme);
    }

    private async Task<ElementTheme> LoadThemeFromSettingsAsync()
    {
        var themeName = await _localSettingsService.ReadSettingAsync<string>(SettingsKey);

        if (Enum.TryParse(themeName, out ElementTheme cacheTheme))
        {
            return cacheTheme;
        }

        return ElementTheme.Default;
    }

    private async Task SaveThemeInSettingsAsync(ElementTheme theme)
    {
        await _localSettingsService.SaveSettingAsync(SettingsKey, theme.ToString());
    }
}
