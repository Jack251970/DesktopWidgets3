﻿using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;

namespace DesktopWidgets3.Widget.Jack251970.DigitalClock.ViewModels;

public partial class DigitalClockViewModel : BaseWidgetViewModel, IWidgetUpdate, IWidgetClose
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

    #region abstract methods

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

    public void OnWidgetClose()
    {
        dispatcherQueueTimer.Stop();
        dispatcherQueueTimer.Tick -= (_, _) => UpdateTime();
    }

    #endregion
}
