using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;

namespace DesktopWidgets3.Widget.DigitalClock.ViewModel;

public partial class ClockViewModel : BaseWidgetViewModel<BaseWidgetSettings>, IWidgetUpdate, IWidgetClose
{
    #region view properties

    [ObservableProperty]
    private string _systemTime = string.Empty;

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
        var systemTime = nowTime.ToString("T");

        SystemTime = systemTime;

        updating = false;
    }

    #region abstract methods

    protected override void LoadSettings(BaseWidgetSettings settings)
    {
        SystemTime = DateTime.Now.ToString("T");
        dispatcherQueueTimer.Start();
    }

    public override BaseWidgetSettings GetSettings()
    {
        return new BaseWidgetSettings();
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
