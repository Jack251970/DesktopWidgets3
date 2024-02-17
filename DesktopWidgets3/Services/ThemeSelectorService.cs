using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Services;

internal class ThemeSelectorService : IThemeSelectorService
{
    private ElementTheme theme = ElementTheme.Default;
    public ElementTheme Theme {
        get => theme;
        set
        {
            if (theme != value)
            {
                ThemeExtensions.RootTheme = theme = value;
                ThemeExtensions.ElementTheme_Changed?.Invoke(this, value);
            }
        } 
    }

    private readonly ILocalSettingsService _localSettingsService;
    private readonly LocalSettingsKeys _localSettingsKeys;

    private string SettingsKey => _localSettingsKeys.ThemeKey;

    private bool _isInitialized;

    public ThemeSelectorService(ILocalSettingsService localSettingsService, IOptions<LocalSettingsKeys> localSettingsKeys)
    {
        _localSettingsService = localSettingsService;
        _localSettingsKeys = localSettingsKeys.Value;
    }

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
        await ThemeHelper.SetRequestedThemeAsync(window, Theme);
    }

    public async Task SetThemeAsync(ElementTheme theme)
    {
        Theme = theme;

        await SetRequestedThemeAsync(App.MainWindow);

        foreach (var window in UIElementExtensions.WindowInstances)
        {
            await SetRequestedThemeAsync(window);
        }

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
