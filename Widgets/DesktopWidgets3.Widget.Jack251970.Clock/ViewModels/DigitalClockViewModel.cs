using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;

namespace DesktopWidgets3.Widget.Jack251970.Clock.ViewModels;

public partial class DigitalClockViewModel : ObservableRecipient
{
    #region view properties

    [ObservableProperty]
    private string _systemTime = string.Empty;

    #endregion

    #region settings

    private string timingFormat = "T";

    #endregion

    private readonly string Id;

    private DispatcherQueueTimer dispatcherQueueTimer = null!;

    public DigitalClockViewModel(string widgetId)
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
        SystemTime = DateTime.Now.ToString(timingFormat);
    }

    #endregion

    #region Settings Methods

    public void LoadSettings(BaseWidgetSettings settings)
    {
        if (settings is DigitalClockSettings digitalClockSettings)
        {
            if (digitalClockSettings.ShowSeconds != (timingFormat == "T"))
            {
                timingFormat = digitalClockSettings.ShowSeconds ? "T" : "t";
            }
        }
    }

    #endregion
}
