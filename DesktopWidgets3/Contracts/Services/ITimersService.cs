using System.Timers;
using DesktopWidgets3.Models.Widget;

namespace DesktopWidgets3.Contracts.Services;

public interface ITimersService
{
    void AddTimerAction(WidgetType type, Action<object?, ElapsedEventArgs> updateTimeDelegate);

    void StartTimer(WidgetType type);

    void StopTimer(WidgetType type);
}
