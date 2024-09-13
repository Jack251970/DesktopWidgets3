using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Widget.Jack251970.AnalogClock.Setting;
using DesktopWidgets3.Widget.Jack251970.AnalogClock.UserControls;
using Microsoft.UI.Dispatching;

namespace DesktopWidgets3.Widget.Jack251970.AnalogClock.ViewModels;

public partial class DigitalClockViewModel : BaseWidgetViewModel, IWidgetUpdate, IWidgetClosing, IBatterySaver
{
    #region view properties

    [ObservableProperty]
    private HandsMode _handsMode = HandsMode.Precise;

    [ObservableProperty]
    private DateTime _dateTime = DateTime.Now;

    [ObservableProperty]
    private string _systemTime = string.Empty;

    #endregion

    #region settings

    private string timingFormat = "T";

    #endregion

    private readonly DispatcherQueueTimer dispatcherQueueTimer;

    public DigitalClockViewModel()
    {
        dispatcherQueueTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        dispatcherQueueTimer.Interval = TimeSpan.FromSeconds(1);
        dispatcherQueueTimer.Tick += (_, _) => UpdateTime();
    }

    private void UpdateTime()
    {
        var nowTime = DateTime.Now;
        var systemTime = nowTime.ToString(timingFormat);

        DateTime = nowTime;
        SystemTime = systemTime;
    }

    #region abstract methods

    protected override void LoadSettings(BaseWidgetSettings settings, bool initialized)
    {
        // initialize or update widget from settings
        if (settings is DigitalClockSettings digitalClockSetting)
        {
            if (digitalClockSetting.ShowSeconds != (timingFormat == "T"))
            {
                timingFormat = digitalClockSetting.ShowSeconds ? "T" : "t";
            }
        }

        // initialize widget
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

    #region battery saver

    public void EnableBatterySaver(bool enable)
    {
        HandsMode = enable ? HandsMode.Normal : HandsMode.Precise;
    }

    #endregion
}
