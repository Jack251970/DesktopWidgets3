using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Contracts.ViewModels;
using DesktopWidgets3.Models.Widget;

namespace DesktopWidgets3.ViewModels.Pages.Widget.Clock;

public partial class ClockViewModel : BaseWidgetViewModel<ClockWidgetSettings>, IWidgetUpdate, IWidgetDispose
{
    #region observable properties

    [ObservableProperty]
    private string _systemTime = string.Empty;

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

    private void UpdateTime()
    {
        _dispatcherQueue.TryEnqueue(() => SystemTime = DateTime.Now.ToString(timingFormat));
    }

    #region abstract methods

    protected override void LoadWidgetSettings(ClockWidgetSettings settings)
    {
        if (settings.ShowSeconds != (timingFormat == "T"))
        {
            timingFormat = settings.ShowSeconds ? "T" : "t";
        }

        SystemTime = DateTime.Now.ToString(timingFormat);
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

    public void DisposeWidget()
    {
        _timersService.RemoveTimerAction(WidgetType.Clock, UpdateTime);
    }

    #endregion
}
