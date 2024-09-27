using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml;

namespace DesktopWidgets3.Services;

internal class ThemeSelectorService(ILocalSettingsService localSettingsService, IOptions<LocalSettingsKeys> localSettingsKeys) : IThemeSelectorService
{
    public ElementTheme Theme { get; set; } = ElementTheme.Default;

    public event EventHandler<ElementTheme>? ThemeChanged;

    private readonly ILocalSettingsService _localSettingsService = localSettingsService;
    private readonly LocalSettingsKeys _localSettingsKeys = localSettingsKeys.Value;

    private string SettingsKey => _localSettingsKeys.ThemeKey;

    private bool _isInitialized;

    public async Task InitializeAsync()
    {
        if (!_isInitialized)
        {
            Theme = await LoadThemeFromSettingsAsync();

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

        ThemeChanged?.Invoke(this, Theme);
    }

    public bool IsDarkTheme()
    {
        // If theme is Default, use the Application.RequestedTheme value
        // https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.elementtheme?view=windows-app-sdk-1.2#fields
        return Theme == ElementTheme.Dark ||
            (Theme == ElementTheme.Default && Application.Current.RequestedTheme == ApplicationTheme.Dark);
    }

    public ElementTheme GetActualTheme()
    {
        return IsDarkTheme() ? ElementTheme.Dark : ElementTheme.Light;
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
