﻿using CommunityToolkit.Mvvm.ComponentModel;

namespace DesktopWidgets3.ViewModels.Pages.Widgets;

public partial class ClockViewModel : BaseWidgetViewModel<ClockWidgetSettings>, IWidgetUpdate, IWidgetClose
{
    #region view properties

    [ObservableProperty]
    private string _systemTime = string.Empty;

    [ObservableProperty]
    private DateTime _dateTime = DateTime.Now;

    #endregion

    #region settings

    private string timingFormat = "T";

    #endregion

    private readonly ITimersService _timersService;

    public ClockViewModel(ITimersService timersService)
    {
        _timersService = timersService;

        timersService.AddTimerAction(WidgetType.Clock, UpdateTime);
    }

    private async void UpdateTime()
    {
        var nowTime = DateTime.Now;
        var systemTime = await Task.Run(() => nowTime.ToString(timingFormat));

        RunOnDispatcherQueue(() => {
            DateTime = nowTime;
            SystemTime = systemTime; 
        });
    }

    #region abstract methods

    protected override void LoadSettings(ClockWidgetSettings settings)
    {
        if (settings.ShowSeconds != (timingFormat == "T"))
        {
            timingFormat = settings.ShowSeconds ? "T" : "t";
        }

        SystemTime = DateTime.Now.ToString(timingFormat);
    }

    public override ClockWidgetSettings GetSettings()
    {
        return new ClockWidgetSettings
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
            _timersService.StartTimer(WidgetType.Clock);
        }
        else
        {
            _timersService.StopTimer(WidgetType.Clock);
        }
        await Task.CompletedTask;
    }

    public void WidgetWindow_Closing()
    {
        _timersService.RemoveTimerAction(WidgetType.Clock, UpdateTime);
    }

    #endregion
}
