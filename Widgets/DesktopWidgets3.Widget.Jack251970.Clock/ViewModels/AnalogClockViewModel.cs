using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;

namespace DesktopWidgets3.Widget.Jack251970.Clock.ViewModels;

public partial class AnalogClockViewModel : ObservableRecipient
{
    #region view properties

    [ObservableProperty]
    private HandsMode _handsMode = HandsMode.Precise;

    [ObservableProperty]
    private DateTime _dateTime = DateTime.Now;

    [ObservableProperty]
    private string _systemTime = DateTime.Now.ToString("T");

    #endregion

    #region settings

    private string timingFormat = "T";

    #endregion

    private readonly string Id;

    private DispatcherQueueTimer dispatcherQueueTimer = null!;

    public AnalogClockViewModel(string widgetId)
    {
        Id = widgetId;
        InitializeAllTimers();
    }

    #region Timer Methods

    private void InitializeAllTimers()
    {
        dispatcherQueueTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        dispatcherQueueTimer.Interval = TimeSpan.FromSeconds(1);
        dispatcherQueueTimer.Tick += (_, _) => UpdateTime();
    }

    public void StartAllTimers()
    {
        dispatcherQueueTimer.Start();
    }

    public void StopAllTimers()
    {
        dispatcherQueueTimer.Stop();
    }

    public void DisposeAllTimers()
    {
        dispatcherQueueTimer.Stop();
        dispatcherQueueTimer.Tick -= (_, _) => UpdateTime();
    }

    #endregion

    #region Update Methods

    private void UpdateTime()
    {
        var nowTime = DateTime.Now;
        var systemTime = nowTime.ToString(timingFormat);

        DateTime = nowTime;
        SystemTime = systemTime;
    }

    #endregion

    #region Settings Methods

    public void LoadSettings(BaseWidgetSettings settings)
    {
        if (settings is AnalogClockSettings analogClockSettings)
        {
            if (analogClockSettings.ShowSeconds != (timingFormat == "T"))
            {
                timingFormat = analogClockSettings.ShowSeconds ? "T" : "t";
            }
        }
    }

    #endregion
}
