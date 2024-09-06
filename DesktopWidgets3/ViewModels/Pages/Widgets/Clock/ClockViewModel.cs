using Clock.UserControls;

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;

namespace DesktopWidgets3.ViewModels.Pages.Widgets;

public partial class ClockViewModel : BaseWidgetViewModel<ClockWidgetSettings>, IWidgetUpdate, IWidgetClose
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

    private readonly IAppSettingsService _appSettingsService;
    private readonly ITimersService _timersService;

    private bool updating = false;

    public ClockViewModel(IAppSettingsService appSettingsService, ITimersService timersService)
    {
        _appSettingsService = appSettingsService;
        _timersService = timersService;

        _appSettingsService.OnBatterySaverChanged += AppSettingsService_OnBatterySaverChanged;
        _timersService.AddTimerAction(WidgetType.Clock, UpdateTime);
    }

    private void AppSettingsService_OnBatterySaverChanged(object? _, bool batterySaver)
    {
        if (batterySaver)
        {
            HandsMode = HandsMode.Normal;
        }
        else
        {
            HandsMode = HandsMode.Precise;
        }
    }

    private void UpdateTime()
    {
        var nowTime = DateTime.Now;
        var systemTime = nowTime.ToString(timingFormat);

        RunOnDispatcherQueue(() =>
        {
            if (updating)
            {
                return;
            }

            updating = true;

            DateTime = nowTime;
            SystemTime = systemTime;

            updating = false;
        }, DispatcherQueuePriority.Normal);
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
