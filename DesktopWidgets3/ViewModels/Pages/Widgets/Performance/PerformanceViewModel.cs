using CommunityToolkit.Mvvm.ComponentModel;

namespace DesktopWidgets3.ViewModels.Pages.Widgets;

public partial class PerformanceViewModel : BaseWidgetViewModel<PerformanceWidgetSettings>, IWidgetUpdate, IWidgetClose
{
    #region view properties

    [ObservableProperty]
    private string _cpuInfo = string.Empty;

    [ObservableProperty]
    private string _gpuInfo = string.Empty;

    [ObservableProperty]
    private string _memoryInfo = string.Empty;

    #endregion

    #region settings

    private bool useCelsius = true;

    #endregion

    private readonly ISystemInfoService _systemInfoService;
    private readonly ITimersService _timersService;

    public PerformanceViewModel(ISystemInfoService systemInfoService, ITimersService timersService)
    {
        _systemInfoService = systemInfoService;
        _timersService = timersService;

        timersService.AddTimerAction(WidgetType.Performance, UpdatePerformance);
    }

    private async void UpdatePerformance()
    {
        var (CpuLoad, CpuTempreture) = await Task.Run(() => _systemInfoService.GetCpuInfo(useCelsius));
        var (GpuLoad, GpuTempreture) = await Task.Run(() => _systemInfoService.GetGpuInfo(useCelsius));
        var (MemoryLoad, MemoryUsedInfo) = await Task.Run(_systemInfoService.GetMemoryInfo);

        RunOnDispatcherQueue(() => {
            CpuInfo = JoinStrings(new List<string> { "cpu", CpuLoad, CpuTempreture }, "\n");
            GpuInfo = JoinStrings(new List<string> { "gpu", GpuLoad, GpuTempreture }, "\n");
            MemoryInfo = JoinStrings(new List<string> { "memory", MemoryLoad, MemoryUsedInfo }, "\n");
        });
    }

    private static string JoinStrings(List<string> strings, string divider)
    {
        return string.Join(divider, strings.Where(s => !string.IsNullOrEmpty(s)));
    }

    #region abstract methods

    protected async override void LoadSettings(PerformanceWidgetSettings settings)
    {
        if (settings.UseCelsius != useCelsius)
        {
            useCelsius = settings.UseCelsius;
        }

        if (CpuInfo == string.Empty && GpuInfo == string.Empty && MemoryInfo == string.Empty)
        {
            var (CpuLoad, CpuTempreture) = await Task.Run(() => _systemInfoService.GetInitCpuInfo(useCelsius));
            var (GpuLoad, GpuTempreture) = await Task.Run(() => _systemInfoService.GetInitGpuInfo(useCelsius));
            var (MemoryLoad, MemoryUsedInfo) = await Task.Run(_systemInfoService.GetInitMemoryInfo);

            CpuInfo = JoinStrings(new List<string> { "cpu", CpuLoad, CpuTempreture }, "\n");
            GpuInfo = JoinStrings(new List<string> { "gpu", GpuLoad, GpuTempreture }, "\n");
            MemoryInfo = JoinStrings(new List<string> { "memory", MemoryLoad, MemoryUsedInfo }, "\n");
        }
    }

    public override PerformanceWidgetSettings GetSettings()
    {
        return new PerformanceWidgetSettings()
        {
            UseCelsius = useCelsius
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
