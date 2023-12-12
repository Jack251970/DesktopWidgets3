using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Models.Widget;

namespace DesktopWidgets3.ViewModels.Pages.Widget.Disk;

public partial class DiskViewModel : BaseWidgetViewModel<DiskWidgetSettings>
{
    #region observable properties

    #endregion

    #region settings

    #endregion

    private readonly ITimersService _timersService;

    public DiskViewModel(ITimersService timersService)
    {
        _timersService = timersService;

        timersService.AddUpdateTimeTimerAction(UpdateDisk);
    }

    private void UpdateDisk(object? sender, EventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() => { });
    }

    #region abstract methods

    protected override void LoadWidgetSettings(DiskWidgetSettings settings)
    {
        
    }

    #endregion
}
