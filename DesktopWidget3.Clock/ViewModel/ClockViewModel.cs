using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;

namespace DesktopWidget3.Clock.ViewModel;

public partial class ClockViewModel : BaseWidgetViewModel, IWidgetUpdate, IWidgetClose
{
    #region view properties

    [ObservableProperty]
    private string _systemTime = string.Empty;

    #endregion

    #region settings

    private string timingFormat = "T";

    #endregion

    private readonly DispatcherQueueTimer dispatcherQueueTimer;

    private bool updating = false;

    public ClockViewModel()
    {
        dispatcherQueueTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        dispatcherQueueTimer.Interval = TimeSpan.FromSeconds(1);
        dispatcherQueueTimer.Tick += (_, _) => UpdateTime();
    }

    private void UpdateTime()
    {
        if (updating)
        {
            return;
        }

        updating = true;

        var nowTime = DateTime.Now;
        var systemTime = nowTime.ToString(timingFormat);

        SystemTime = systemTime;

        updating = false;
    }

    #region interfaces

    public async Task EnableUpdate(bool enable)
    {
        if (enable)
        {
            dispatcherQueueTimer.Start();
        }
        else
        {
            dispatcherQueueTimer.Stop();
        }

        await Task.CompletedTask;
    }

    public void WidgetWindow_Closing()
    {
        dispatcherQueueTimer.Stop();
    }

    #endregion
}
