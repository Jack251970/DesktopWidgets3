namespace DesktopWidgets3.Widget.Contracts.Services;

public interface ISettingsService
{
    bool BatterySaver { get; }

    event Action<bool>? OnBatterySaverChanged;
}
