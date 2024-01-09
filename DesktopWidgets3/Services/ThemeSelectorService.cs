using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml;

using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.Models;
using DesktopWidgets3.Files.App.Helpers;

namespace DesktopWidgets3.Services;

public class ThemeSelectorService : IThemeSelectorService
{
    private ElementTheme theme = ElementTheme.Default;
    public ElementTheme Theme {
        get => theme;
        set
        {
            if (theme != value)
            {
                theme = value;

                // Update theme for dialogs of Files
                ThemeHelper.RootTheme = value;
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
        if (window.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = Theme;

            TitleBarHelper.UpdateTitleBar(Theme);
        }

        await Task.CompletedTask;
    }

    public async Task SetThemeAsync(ElementTheme theme)
    {
        Theme = theme;

        await SetRequestedThemeAsync(App.MainWindow);
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
