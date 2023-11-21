using System.Timers;

namespace DesktopWidgets3.Contracts.Services;

public interface ITimersService
{
    void InitializeUpdateTimeTimer(Action<object?, ElapsedEventArgs> updateTimeDelegate);

    void InitializeStopTimingTimer(Action<object?, ElapsedEventArgs> stopTimingDelegate);

    void InitializeBreakReminderTimer(Action<object?, ElapsedEventArgs> remindBreakDelegate);

    void InitializeKillProcessesTimer(Action<object?, ElapsedEventArgs> killProcessesDelegate);

    void StartUpdateTimeStopTimingTimerAsync();

    void StopUpdateTimeStopTimingTimer();

    void StartBreakReminderKillProcessesTimerAsync();

    void StopBreakReminderKillProcessesTimerAsync();

    void StartUpdateTimeTimer();

    void StopUpdateTimeTimer();

    void StartAllTimersAsync();

    void StopAllTimers();
}
