using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidget3.Clock.Setting;
using Microsoft.UI.Dispatching;

namespace DesktopWidget3.Clock.ViewModel;

public partial class ClockViewModel : BaseWidgetViewModel<ClockSetting>, IWidgetUpdate, IWidgetClose
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

    #region abstract methods

    protected override void LoadSettings(ClockSetting settings)
    {
        if (settings.ShowSeconds != (timingFormat == "T"))
        {
            timingFormat = settings.ShowSeconds ? "T" : "t";
        }

        SystemTime = DateTime.Now.ToString(timingFormat);
        dispatcherQueueTimer.Start();
    }

    public override ClockSetting GetSettings()
    {
        return new ClockSetting
        {
            ShowSeconds = timingFormat == "T"
        };
    }

    #endregion

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
