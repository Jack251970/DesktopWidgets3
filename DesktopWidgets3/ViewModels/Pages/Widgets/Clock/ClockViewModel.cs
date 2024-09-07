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

    private readonly DispatcherQueueTimer dispatcherQueueTimer;

    private bool updating = false;

    public ClockViewModel(IAppSettingsService appSettingsService)
    {
        _appSettingsService = appSettingsService;

        _appSettingsService.OnBatterySaverChanged += AppSettingsService_OnBatterySaverChanged;

        dispatcherQueueTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        dispatcherQueueTimer.Interval = TimeSpan.FromSeconds(1);
        dispatcherQueueTimer.Tick += (_, _) => UpdateTime();
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
        if (updating)
        {
            return;
        }

        updating = true;

        var nowTime = DateTime.Now;
        var systemTime = nowTime.ToString(timingFormat);

        DateTime = nowTime;
        SystemTime = systemTime;

        updating = false;
    }

    #region abstract methods

    protected override void LoadSettings(ClockWidgetSettings settings)
    {
        if (settings.ShowSeconds != (timingFormat == "T"))
        {
            timingFormat = settings.ShowSeconds ? "T" : "t";
        }

        SystemTime = DateTime.Now.ToString(timingFormat);
        dispatcherQueueTimer.Start();
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
