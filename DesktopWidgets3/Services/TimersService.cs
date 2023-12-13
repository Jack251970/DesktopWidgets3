using System.Timers;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Models.Widget;
using Timer = System.Timers.Timer;

namespace DesktopWidgets3.Services;

public class TimersService : ITimersService
{
    private readonly Timer _updateTimeTimer = new(1000);
    private readonly Timer _updateNetworkTimer = new(1000);

    public TimersService()
    {
        InitializeTimers();
    }

    public void AddTimerAction(WidgetType type, Action<object?, ElapsedEventArgs> updateTimeDelegate)
    {
        
    }

    public void AddUpdateTimeTimerAction(Action<object?, ElapsedEventArgs> updateTimeDelegate)
    {
        _updateTimeTimer.Enabled = false;
        _updateTimeTimer.Elapsed += (sender, e) => updateTimeDelegate(sender, e);
        _updateTimeTimer.Enabled = true;
    }

    public void StartTimer(WidgetType type)
    {
    
    }

    public void StartUpdateTimeTimer()
    {
        if (!_updateTimeTimer.Enabled)
        {
            _updateTimeTimer.Enabled = true;
        }
    }

    public void StopTimer(WidgetType type)
    {
    
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
        _updateNetworkTimer.AutoReset = true;
        _updateNetworkTimer.Enabled = false;
    }
}
