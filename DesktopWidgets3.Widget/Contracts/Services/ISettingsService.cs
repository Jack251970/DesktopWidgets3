namespace DesktopWidgets3.Widget;

public interface ISettingsService
{
    bool BatterySaver { get; }

    event Action<bool>? OnBatterySaverChanged;
}
