using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;

namespace DesktopWidgets3.Widget.Jack251970.AnalogClock.ViewModels;

public partial class AnalogClockViewModel : BaseWidgetViewModel, IWidgetUpdate, IWidgetClosing
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

    private readonly WidgetInitContext Context;

    private readonly DispatcherQueueTimer dispatcherQueueTimer;

    public AnalogClockViewModel(WidgetInitContext context)
    {
        Context = context;
        Context.API.OnBatterySaverChanged += OnBatterySaverChanged;

        dispatcherQueueTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        dispatcherQueueTimer.Interval = TimeSpan.FromSeconds(1);
        dispatcherQueueTimer.Tick += (_, _) => UpdateTime();
    }

    private void OnBatterySaverChanged(bool enable)
    {
        HandsMode = enable ? HandsMode.Normal : HandsMode.Precise;
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
        if (settings is AnalogClockSettings analogClockSettings)
        {
            if (analogClockSettings.ShowSeconds != (timingFormat == "T"))
            {
                timingFormat = analogClockSettings.ShowSeconds ? "T" : "t";
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
        Context.API.OnBatterySaverChanged -= OnBatterySaverChanged;
        dispatcherQueueTimer.Stop();
        dispatcherQueueTimer.Tick -= (_, _) => UpdateTime();
    }

    #endregion
}
