namespace DesktopWidgets3.ViewModels.Pages.Widgets;

public partial class DiskViewModel : BaseWidgetViewModel<DiskWidgetSettings>, IWidgetUpdate, IWidgetClose
{
    #region view properties

    #endregion

    #region settings

    #endregion

    private readonly ISystemInfoService _systemInfoService;
    private readonly ITimersService _timersService;

    public DiskViewModel(ISystemInfoService systemInfoService, ITimersService timersService)
    {
        _systemInfoService = systemInfoService;
        _timersService = timersService;

        timersService.AddTimerAction(WidgetType.Disk, UpdateDisk);
    }

    private void UpdateDisk()
    {
        var a = _systemInfoService.GetDiskInfo();

        RunOnDispatcherQueue(() => { });
    }

    #region abstract methods

    protected override void LoadSettings(DiskWidgetSettings settings)
    {
        
    }

    public override DiskWidgetSettings GetSettings()
    {
        return new DiskWidgetSettings()
        {

        };
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

    public void WidgetWindow_Closing()
    {
        _timersService.RemoveTimerAction(WidgetType.Disk, UpdateDisk);
    }

    #endregion
}
