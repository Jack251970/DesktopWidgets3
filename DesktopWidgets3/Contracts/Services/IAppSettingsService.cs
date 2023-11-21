namespace DesktopWidgets3.Contracts.Services;

public interface IAppSettingsService
{
    bool IsLocking
    {
        get; set;
    }

    bool IsRelaxing
    {
        get; set;
    }

    bool IsTiming
    {
        get;
    }

    bool BatterySaver
    {
        get; set;
    }

    bool ForbidQuit
    {
        get; set;
    }

    Task<bool> GetBatterySaverAsync();

    Task SetBatterySaverAsync(bool value);

    Task<bool> GetShowSecondsAsync();

    Task SetShowSecondsAsync(bool value);

    Task<bool> GetStrictModeAsync();

    Task SetStrictModeAsync(bool value);

    Task<int> GetBreakIntervalAsync();

    Task SetBreakIntervalAsync(int value);

    Task<bool> GetForbidQuitAsync();

    Task SetForbidQuitAsync(bool value);

    Task<Dictionary<string, object>> GetLockPeriod();

    Task SaveLockPeriod(DateTime startLockTime, DateTime endLockTime);

    List<string> GetBlockList();

    Task SaveBlockList(string exeName, bool isBlock);
}
