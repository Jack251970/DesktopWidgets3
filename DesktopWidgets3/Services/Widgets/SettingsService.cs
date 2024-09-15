namespace DesktopWidgets3.Services.Widgets;

internal class SettingsService : ISettingsService
{
    private static IAppSettingsService AppSettingsService => DependencyExtensions.GetRequiredService<IAppSettingsService>();

    bool ISettingsService.BatterySaver => AppSettingsService.BatterySaver;

    event Action<bool>? ISettingsService.OnBatterySaverChanged
    {
        add => AppSettingsService.OnBatterySaverChanged += value;
        remove => AppSettingsService.OnBatterySaverChanged -= value;
    }
}
