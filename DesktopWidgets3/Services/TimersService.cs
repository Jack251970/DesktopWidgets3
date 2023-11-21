using System.Timers;
using DesktopWidgets3.Contracts.Services;
using Timer = System.Timers.Timer;

namespace DesktopWidgets3.Services;

public class TimersService : ITimersService
{
    private readonly Timer _updateTimeTimer = new();
    private readonly Timer _stopTimingTimer = new(1000);
    private readonly Timer _breakReminderTimer = new();
    private readonly Timer _killProcessesTimer = new(1000);

    private readonly IAppSettingsService _appSettingsService;

    private bool _isUpdateTimeTimerInitialized = false;
    private bool _isStopTimingTimerInitialized = false;
    private bool _isBreakReminderTimerInitialized = false;
    private bool _isKillProcessesTimerInitialized = false;

    public TimersService(IAppSettingsService appSettingsService)
    {
        _appSettingsService = appSettingsService;
    }

    public void InitializeUpdateTimeTimer(Action<object?, ElapsedEventArgs> updateTimeDelegate)
    {
        if (!_isUpdateTimeTimerInitialized)
        {
            _updateTimeTimer.Elapsed += (sender, e) => updateTimeDelegate(sender, e);
            _updateTimeTimer.AutoReset = true;
            _updateTimeTimer.Enabled = false;

            _isUpdateTimeTimerInitialized = true;
        }
    }

    public void InitializeStopTimingTimer(Action<object?, ElapsedEventArgs> stopTimingDelegate)
    {
        if (!_isStopTimingTimerInitialized)
        {
            _stopTimingTimer.Elapsed += (sender, e) => stopTimingDelegate(sender, e);
            _stopTimingTimer.AutoReset = true;
            _stopTimingTimer.Enabled = false;

            _isStopTimingTimerInitialized = true;
        }
    }

    public void InitializeBreakReminderTimer(Action<object?, ElapsedEventArgs> remindBreakDelegate)
    {
        if (!_isBreakReminderTimerInitialized)
        {
            _breakReminderTimer.Elapsed += (sender, e) => remindBreakDelegate(sender, e);
            _breakReminderTimer.AutoReset = true;
            _breakReminderTimer.Enabled = false;

            _isBreakReminderTimerInitialized = true;
        }
    }

    public void InitializeKillProcessesTimer(Action<object?, ElapsedEventArgs> killProcessesDelegate)
    {
        if (!_isKillProcessesTimerInitialized)
        {
            _killProcessesTimer.Elapsed += (sender, e) => killProcessesDelegate(sender, e);
            _killProcessesTimer.AutoReset = true;
            _killProcessesTimer.Enabled = false;

            _isKillProcessesTimerInitialized = true;
        }
    }

    public async void StartUpdateTimeStopTimingTimerAsync()
    {
        var batterySaver = _appSettingsService.BatterySaver;
        var showSeconds = await _appSettingsService.GetShowSecondsAsync();
        if (batterySaver)
        {
            _updateTimeTimer.Interval = showSeconds ? 1000 : 6000;
            _stopTimingTimer.Interval = 3000;
        }
        else
        {
            _updateTimeTimer.Interval = showSeconds ? 1000 : 3000;
            _stopTimingTimer.Interval = 1000;
        }
        _updateTimeTimer.Enabled = _stopTimingTimer.Enabled = true;
    }

    public void StopUpdateTimeStopTimingTimer()
    {
        _updateTimeTimer.Enabled = _stopTimingTimer.Enabled = false;
    }

    public async void StartBreakReminderKillProcessesTimerAsync()
    {
        var breakInterval = await _appSettingsService.GetBreakIntervalAsync();
        var strictMode = await _appSettingsService.GetStrictModeAsync();
        if (strictMode)
        {
            _killProcessesTimer.Enabled = true;
        }
        _breakReminderTimer.Interval = 60000 * breakInterval;
        await Task.Delay(1000);
        _breakReminderTimer.Enabled = true;
    }

    public async void StopBreakReminderKillProcessesTimerAsync()
    {
        var strictMode = await _appSettingsService.GetStrictModeAsync();
        _breakReminderTimer.Enabled = false;
        if (strictMode)
        {
            _killProcessesTimer.Enabled = false;
        }
    }

    public void StartUpdateTimeTimer()
    {
        if (!_updateTimeTimer.Enabled && _appSettingsService.IsTiming)
        {
            _updateTimeTimer.Enabled = true;
        }  
    }

    public void StopUpdateTimeTimer()
    {
        if (_updateTimeTimer.Enabled && _appSettingsService.BatterySaver)
        {
            _updateTimeTimer.Enabled = false;
        }
    }

    public async void StartAllTimersAsync()
    {
        if (_appSettingsService.IsTiming)
        {
            _updateTimeTimer.Enabled = _stopTimingTimer.Enabled = true;
        }
        if (_appSettingsService.IsLocking)
        {
            _breakReminderTimer.Enabled = true;
            var strictMode = await _appSettingsService.GetStrictModeAsync();
            if (strictMode)
            {
                _killProcessesTimer.Enabled = true;
            }
        }
    }

    public void StopAllTimers()
    {
        _updateTimeTimer.Enabled = _stopTimingTimer.Enabled = _breakReminderTimer.Enabled = _killProcessesTimer.Enabled = false;
    }
}
