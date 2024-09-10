using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Widget.DigitalClock.Setting;
using Microsoft.UI.Dispatching;

namespace DesktopWidgets3.Widget.DigitalClock.ViewModels;

public partial class DigitalClockViewModel : BaseWidgetViewModel, IWidgetUpdate, IWidgetClosing
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

    public DigitalClockViewModel()
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

    public override void LoadSettings(BaseWidgetSettings settings, bool initialized)
    {
        if (settings is DigitalClockSettings digitalClockSetting)
        {
            if (digitalClockSetting.ShowSeconds != (timingFormat == "T"))
            {
                timingFormat = digitalClockSetting.ShowSeconds ? "T" : "t";
            }
        }

        if (initialized)
        {
            SystemTime = DateTime.Now.ToString(timingFormat);
            dispatcherQueueTimer.Start();
        }
    }

    #endregion

    #region widget update

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

    #endregion

    #region widget closing

    public void WidgetWindow_Closing()
    {
        dispatcherQueueTimer.Stop();
        dispatcherQueueTimer.Tick -= (_, _) => UpdateTime();
    }

    #endregion
}
