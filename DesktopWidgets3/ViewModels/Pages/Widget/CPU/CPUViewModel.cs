using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Models.Widget;

namespace DesktopWidgets3.ViewModels.Pages.Widget.CPU;

public partial class CPUViewModel : BaseWidgetViewModel<CPUWidgetSettings>
{
    #region observable properties

    #endregion

    #region settings

    #endregion

    private readonly ITimersService _timersService;

    public CPUViewModel(ITimersService timersService)
    {
        _timersService = timersService;

        timersService.AddUpdateTimeTimerAction(UpdateCPU);
    }

    private void UpdateCPU(object? sender, EventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() => { });
    }

    #region abstract methods

    protected override void LoadWidgetSettings(CPUWidgetSettings settings)
    {
        
    }

    #endregion
}
