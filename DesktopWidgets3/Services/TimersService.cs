using DesktopWidgets3.Models.Widget;
using Timer = System.Timers.Timer;

namespace DesktopWidgets3.Services;

public class TimersService : ITimersService
{
    private readonly Dictionary<WidgetType, Timer> TimersDict = new();

    public TimersService()
    {

    }

    public void AddTimerAction(WidgetType type, Action timeDelegate)
    {
        var timer = GetWidgetTimer(type);
        if (timer != null)
        {
            timer.Elapsed += (s, e) => timeDelegate();
        }
    }

    public void RemoveTimerAction(WidgetType type, Action timeDelegate)
    {
        var timer = GetWidgetTimer(type);
        if (timer != null)
        {
            timer.Elapsed -= (s, e) => timeDelegate();
        }
    }

    public void StartTimer(WidgetType type)
    {
        var timer = GetWidgetTimer(type);
        timer?.Start();
    }

    public void StopTimer(WidgetType type)
    {
        var timer = GetWidgetTimer(type);
        timer?.Stop();
    }

    private Timer? GetWidgetTimer(WidgetType type)
    {
        if (TimersDict.TryGetValue(type, out var timer))
        {
            return timer;
        }
        else
        {
            timer = type switch
            {
                WidgetType.Clock => new Timer(1000),
                WidgetType.Network => new Timer(1000),
                _ => null,
            };
            if (timer != null)
            {
                timer.AutoReset = true;
                timer.Enabled = false;
                TimersDict.Add(type, timer);
            }
            return timer;
        }
    }
}
