namespace DesktopWidgets3.Services.Widgets;

internal class SettingsService : ISettingsService
{
    private readonly IAppSettingsService _appSettingsService = DependencyExtensions.GetRequiredService<IAppSettingsService>();

    bool ISettingsService.BatterySaver => _appSettingsService.BatterySaver;

    event Action<bool>? ISettingsService.OnBatterySaverChanged
    {
        add => _appSettingsService.OnBatterySaverChanged += value;
        remove => _appSettingsService.OnBatterySaverChanged -= value;
    }
}
