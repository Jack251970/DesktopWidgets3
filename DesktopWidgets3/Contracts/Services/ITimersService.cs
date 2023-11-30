using System.Timers;

namespace DesktopWidgets3.Contracts.Services;

public interface ITimersService
{
    void AddUpdateTimeTimerAction(Action<object?, ElapsedEventArgs> updateTimeDelegate);

    void StartUpdateTimeTimer();

    void StopUpdateTimeTimer();
}
