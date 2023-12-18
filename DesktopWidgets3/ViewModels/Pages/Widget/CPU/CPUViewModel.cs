using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Contracts.ViewModels;
using DesktopWidgets3.Models.Widget;

namespace DesktopWidgets3.ViewModels.Pages.Widget.CPU;

public partial class CPUViewModel : BaseWidgetViewModel<CPUWidgetSettings>, IWidgetUpdate, IWidgetClose
{
    #region observable properties

    #endregion

    #region settings

    #endregion

    private readonly ITimersService _timersService;

    public CPUViewModel(ITimersService timersService)
    {
        _timersService = timersService;

        timersService.AddTimerAction(WidgetType.CPU, UpdateCPU);
    }

    private void UpdateCPU()
    {
        RunOnDispatcherQueue(() => { });
    }

    #region abstract methods

    protected override void LoadSettings(CPUWidgetSettings settings)
    {
        
    }

    protected override CPUWidgetSettings GetSettings()
    {
        return new CPUWidgetSettings()
        {

        };
    }

    #endregion

    #region interfaces

    public async Task EnableUpdate(bool enable)
    {
        if (enable)
        {
            _timersService.StartTimer(WidgetType.CPU);
        }
        else
        {
            _timersService.StopTimer(WidgetType.CPU);
        }
        await Task.CompletedTask;
    }

    public void WidgetWindow_Closing()
    {
        _timersService.RemoveTimerAction(WidgetType.CPU, UpdateCPU);
    }

    #endregion
}
