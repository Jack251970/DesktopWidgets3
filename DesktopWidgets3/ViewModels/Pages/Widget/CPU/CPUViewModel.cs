using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Contracts.ViewModels;
using DesktopWidgets3.Models.Widget;

namespace DesktopWidgets3.ViewModels.Pages.Widget.CPU;

public partial class CPUViewModel : BaseWidgetViewModel<CPUWidgetSettings>, IWidgetUpdate, IWidgetDispose
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
        _dispatcherQueue.TryEnqueue(() => { });
    }

    #region abstract methods

    protected override void LoadWidgetSettings(CPUWidgetSettings settings)
    {
        
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

    public void DisposeWidget()
    {
        _timersService.RemoveTimerAction(WidgetType.CPU, UpdateCPU);
    }

    #endregion
}
