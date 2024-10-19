using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;

namespace DesktopWidgets3.Widget.Jack251970.Clock.ViewModels;

public partial class DigitalClockViewModel : BaseWidgetViewModel, IWidgetUpdate, IWidgetWindowClose
{
    #region view properties

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
        SystemTime = DateTime.Now.ToString(timingFormat);
    }

    #region Abstract Methods

    protected override void LoadSettings(BaseWidgetSettings settings, bool initialized)
    {
        // initialize or update widget from settings
        if (settings is DigitalClockSettings digitalClockSettings)
        {
            if (digitalClockSettings.ShowSeconds != (timingFormat == "T"))
            {
                timingFormat = digitalClockSettings.ShowSeconds ? "T" : "t";
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

    #region IWidgetUpdate

    public void EnableUpdate(bool enable)
    {
        if (enable)
        {
            dispatcherQueueTimer.Start();
        }
        else
        {
            dispatcherQueueTimer.Stop();
        }
    }

    #endregion

    #region IWidgetWindowClose

    public void WidgetWindowClosing()
    {
        dispatcherQueueTimer.Stop();
        dispatcherQueueTimer.Tick -= (_, _) => UpdateTime();
    }

    #endregion
}
