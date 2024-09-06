using CommunityToolkit.Mvvm.ComponentModel;

namespace DesktopWidgets3.ViewModels.Pages.Widgets;

public partial class PerformanceViewModel : BaseWidgetViewModel<PerformanceWidgetSettings>, IWidgetUpdate, IWidgetClose
{
    #region view properties

    [ObservableProperty]
    private string _cpuLeftInfo = string.Empty;

    [ObservableProperty]
    private string _cpuRightInfo = string.Empty;

    [ObservableProperty]
    private double _cpuLoadValue = 0;

    [ObservableProperty]
    private string _gpuLeftInfo = string.Empty;

    [ObservableProperty]
    private string _gpuRightInfo = string.Empty;

    [ObservableProperty]
    private double _gpuLoadValue = 0;

    [ObservableProperty]
    private string _memoryLeftInfo = string.Empty;

    [ObservableProperty]
    private string _memoryRightInfo = string.Empty;

    [ObservableProperty]
    private double _memoryLoadValue = 0;

    #endregion

    #region settings

    private bool useCelsius = true;

    #endregion

    private readonly ISystemInfoService _systemInfoService;
    private readonly ITimersService _timersService;

    private bool updating = false;

    public PerformanceViewModel(ISystemInfoService systemInfoService, ITimersService timersService)
    {
        _systemInfoService = systemInfoService;
        _timersService = timersService;

        timersService.AddTimerAction(WidgetType.Performance, UpdatePerformance);
    }

    private async void UpdatePerformance()
    {
        await UpdateCards(false);
    }

    private async Task UpdateCards(bool isInit)
    {
        try
        {
            var cpuLoad = string.Empty;
            var cpuSpeed = string.Empty;
            double cpuLoadValue = 0;
            var gpuName = string.Empty;
            var gpuLoad = string.Empty;
            var gpuTempreture = string.Empty;
            double gpuLoadValue = 0;
            var memoryLoad = string.Empty;
            var memoryUsedInfo = string.Empty;
            double memoryLoadValue = 0;

            if (isInit)
            {
                var cpuTask = Task.Run(_systemInfoService.GetInitCpuInfo);
                var gpuTask = Task.Run(() => _systemInfoService.GetInitGpuInfo(useCelsius));
                var memoryTask = Task.Run(_systemInfoService.GetInitMemoryInfo);

                await Task.WhenAll(cpuTask, gpuTask, memoryTask);

                (cpuLoad, cpuLoadValue, cpuSpeed) = cpuTask.Result;
                (gpuName, gpuLoad, gpuLoadValue, gpuTempreture) = gpuTask.Result;
                (memoryLoad, memoryLoadValue, memoryUsedInfo) = memoryTask.Result;
            }
            else
            {
                var cpuTask = Task.Run(_systemInfoService.GetCpuInfo);
                var gpuTask = Task.Run(() => _systemInfoService.GetGpuInfo(useCelsius));
                var memoryTask = Task.Run(_systemInfoService.GetMemoryInfo);

                await Task.WhenAll(cpuTask, gpuTask, memoryTask);

                (cpuLoad, cpuLoadValue, cpuSpeed) = cpuTask.Result;
                (gpuName, gpuLoad, gpuLoadValue, gpuTempreture) = gpuTask.Result;
                (memoryLoad, memoryLoadValue, memoryUsedInfo) = memoryTask.Result;
            }

            RunOnDispatcherQueue(() =>
            {
                if (updating)
                {
                    return;
                }

                updating = true;

                CpuLeftInfo = "Cpu".GetLocalized();
                CpuRightInfo = string.IsNullOrEmpty(cpuSpeed) ? cpuLoad : cpuSpeed;
                CpuLoadValue = cpuLoadValue * 100;
                GpuLeftInfo = string.IsNullOrEmpty(gpuName) ? "Gpu".GetLocalized() : "Gpu".GetLocalized() + $" ({gpuName})";
                GpuRightInfo = string.IsNullOrEmpty(gpuTempreture) ? gpuLoad : gpuTempreture;
                GpuLoadValue = gpuLoadValue * 100;
                MemoryLeftInfo = "Memory".GetLocalized();
                MemoryRightInfo = string.IsNullOrEmpty(memoryUsedInfo) ? memoryLoad : memoryUsedInfo;
                MemoryLoadValue = memoryLoadValue * 100;

                updating = false;
            });
        }
        catch (Exception e)
        {
            LogExtensions.LogError(e, "Error updating performance widget.");
        }
    }

    #region abstract methods

    protected async override void LoadSettings(PerformanceWidgetSettings settings)
    {
        if (settings.UseCelsius != useCelsius)
        {
            useCelsius = settings.UseCelsius;
        }

        if (CpuLeftInfo == string.Empty)
        {
            await UpdateCards(true);
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
