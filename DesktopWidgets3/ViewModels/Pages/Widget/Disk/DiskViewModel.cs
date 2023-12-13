using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Contracts.ViewModels;
using DesktopWidgets3.Models.Widget;

namespace DesktopWidgets3.ViewModels.Pages.Widget.Disk;

public partial class DiskViewModel : BaseWidgetViewModel<DiskWidgetSettings>, IWidgetUpdate, IWidgetDispose
{
    #region observable properties

    #endregion

    #region settings

    #endregion

    private readonly ITimersService _timersService;

    public DiskViewModel(ITimersService timersService)
    {
        _timersService = timersService;

        timersService.AddTimerAction(WidgetType.Disk, UpdateDisk);
    }

    private void UpdateDisk()
    {
        _dispatcherQueue.TryEnqueue(() => { });
    }

    #region abstract methods

    protected override void LoadWidgetSettings(DiskWidgetSettings settings)
    {
        
    }

    #endregion

    #region interfaces

    public async Task EnableUpdate(bool enable)
    {
        if (enable)
        {
            _timersService.StartTimer(WidgetType.Disk);
        }
        else
        {
            _timersService.StopTimer(WidgetType.Disk);
        }
        await Task.CompletedTask;
    }

    public void DisposeWidget()
    {
        _timersService.RemoveTimerAction(WidgetType.Disk, UpdateDisk);
    }

    #endregion
}
