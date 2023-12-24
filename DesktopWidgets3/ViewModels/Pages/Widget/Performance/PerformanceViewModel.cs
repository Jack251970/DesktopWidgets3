using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Contracts.ViewModels;
using DesktopWidgets3.Models.Widget;

namespace DesktopWidgets3.ViewModels.Pages.Widget;

public partial class PerformanceViewModel : BaseWidgetViewModel<PerformanceWidgetSettings>, IWidgetUpdate, IWidgetClose
{
    #region view properties

    #endregion

    #region settings

    #endregion

    private readonly ITimersService _timersService;

    public PerformanceViewModel(ITimersService timersService)
    {
        _timersService = timersService;

        timersService.AddTimerAction(WidgetType.Performance, UpdatePerformance);
    }

    private void UpdatePerformance()
    {
        RunOnDispatcherQueue(() => { });
    }

    #region abstract methods

    protected override void LoadSettings(PerformanceWidgetSettings settings)
    {
        
    }

    public override PerformanceWidgetSettings GetSettings()
    {
        return new PerformanceWidgetSettings()
        {

        };
    }

    #endregion

    #region interfaces

    public async Task EnableUpdate(bool enable)
    {
        if (enable)
        {
            _timersService.StartTimer(WidgetType.Performance);
        }
        else
        {
            _timersService.StopTimer(WidgetType.Performance);
        }
        await Task.CompletedTask;
    }

    public void WidgetWindow_Closing()
    {
        _timersService.RemoveTimerAction(WidgetType.Performance, UpdatePerformance);
    }

    #endregion
}
