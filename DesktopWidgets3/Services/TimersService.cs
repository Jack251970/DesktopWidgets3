using System.Timers;
using DesktopWidgets3.Contracts.Services;
using Timer = System.Timers.Timer;

namespace DesktopWidgets3.Services;

public class TimersService : ITimersService
{
    private readonly Timer _updateTimeTimer = new();

    private readonly IAppSettingsService _appSettingsService;

    public TimersService(IAppSettingsService appSettingsService)
    {
        _appSettingsService = appSettingsService;

        InitializeTimers();
    }

    public void AddUpdateTimeTimerAction(Action<object?, ElapsedEventArgs> updateTimeDelegate)
    {
        _updateTimeTimer.Elapsed += (sender, e) => updateTimeDelegate(sender, e);
    }

    public void StartUpdateTimeTimer()
    {
        if (!_updateTimeTimer.Enabled)
        {
            var batterySaver = _appSettingsService.BatterySaver;
            _updateTimeTimer.Interval = batterySaver ? 500 : 1000;
            _updateTimeTimer.Enabled = true;
        }
    }

    public void StopUpdateTimeTimer()
    {
        if (_updateTimeTimer.Enabled)
        {
            _updateTimeTimer.Enabled = false;
        }
    }

    private void InitializeTimers()
    {
        _updateTimeTimer.AutoReset = true;
        _updateTimeTimer.Enabled = false;
    }
}
