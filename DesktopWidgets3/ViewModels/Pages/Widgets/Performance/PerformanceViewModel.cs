using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Widget.Contracts.ViewModel;

namespace DesktopWidgets3.ViewModels.Pages.Widgets;

public partial class PerformanceViewModel : BaseWidgetViewModel<PerformanceWidgetSettings>, IWidgetUpdate, IWidgetClose
{
    private static string ClassName => typeof(PerformanceViewModel).Name;

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

    private bool updating = false;

    public PerformanceViewModel(ISystemInfoService systemInfoService)
    {
        _systemInfoService = systemInfoService;

        _systemInfoService.RegisterUpdatedCallback(HardwareType.CPU, UpdateCPU);
        _systemInfoService.RegisterUpdatedCallback(HardwareType.GPU, UpdateGPU);
        _systemInfoService.RegisterUpdatedCallback(HardwareType.Memory, UpdateMemory);
    }

    private void UpdateCPU()
    {
        try
        {
            var cpuStats = _systemInfoService.GetCPUStats();

            if (cpuStats == null)
            {
                return;
            }

            var cpuUsage = cpuStats.CpuUsage;
            var cpuSpeed = FormatUtils.FormatCpuSpeed(cpuStats.CpuSpeed);

            TryEnqueue(() =>
            {
                if (updating)
                {
                    return;
                }

                updating = true;

                CpuLeftInfo = "Cpu".GetLocalized();
                CpuRightInfo = string.IsNullOrEmpty(cpuSpeed) ? FormatUtils.FormatPercentage(cpuUsage) : cpuSpeed;
                CpuLoadValue = cpuUsage * 100;

                updating = false;
            });
        }
        catch (Exception e)
        {
            LogExtensions.LogError(ClassName, e, "Error updating performance widget.");
        }
    }

    private void UpdateGPU()
    {
        try
        {
            var gpuStats = _systemInfoService.GetGPUStats();

            if (gpuStats == null)
            {
                return;
            }

            // TODO: Add actite index support.
            var _gpuActiveIndex = 0;
            var gpuName = gpuStats.GetGPUName(_gpuActiveIndex);
            var gpuUsage = gpuStats.GetGPUUsage(_gpuActiveIndex);
            var gpuTemperature = gpuStats.GetGPUTemperature(_gpuActiveIndex);

            TryEnqueue(() =>
            {
                if (updating)
                {
                    return;
                }

                updating = true;

                GpuLeftInfo = string.IsNullOrEmpty(gpuName) ? "Gpu".GetLocalized() : "Gpu".GetLocalized() + $" ({gpuName})";
                GpuRightInfo = gpuTemperature == 0 ? FormatUtils.FormatPercentage(gpuUsage) : FormatUtils.FormatTemperature(gpuTemperature, useCelsius);
                GpuLoadValue = gpuUsage * 100;
                
                updating = false;
            });
        }
        catch (Exception e)
        {
            LogExtensions.LogError(ClassName, e, "Error updating performance widget.");
        }
    }

    private void UpdateMemory()
    {
        try
        {
            var memoryStats = _systemInfoService.GetMemoryStats();

            if (memoryStats == null)
            {
                return;
            }

            var usedMem = memoryStats.UsedMem;
            var memoryUsage = memoryStats.MemUsage;
            var allMem = memoryStats.AllMem;
            var memoryUsedInfo = FormatUtils.FormatUsedInfoByte(usedMem, allMem);

            TryEnqueue(() =>
            {
                if (updating)
                {
                    return;
                }

                updating = true;

                MemoryLeftInfo = "Memory".GetLocalized();
                MemoryRightInfo = allMem == 0 ? FormatUtils.FormatPercentage(memoryUsage) : memoryUsedInfo;
                MemoryLoadValue = memoryUsage * 100;

                updating = false;
            });
        }
        catch (Exception e)
        {
            LogExtensions.LogError(ClassName, e, "Error updating performance widget.");
        }
    }

    #region abstract methods

    protected override void LoadSettings(PerformanceWidgetSettings settings)
    {
        if (settings.UseCelsius != useCelsius)
        {
            useCelsius = settings.UseCelsius;
        }

        if (CpuLeftInfo == string.Empty)
        {
            CpuLeftInfo = "--";
            CpuRightInfo = "--";
            GpuLeftInfo = "--";
            GpuRightInfo = "--";
            MemoryLeftInfo = "--";
            MemoryRightInfo = "--";
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
            _systemInfoService.RegisterUpdatedCallback(HardwareType.CPU, UpdateCPU);
            _systemInfoService.RegisterUpdatedCallback(HardwareType.GPU, UpdateGPU);
            _systemInfoService.RegisterUpdatedCallback(HardwareType.Memory, UpdateMemory);
        }
        else
        {
            _systemInfoService.UnregisterUpdatedCallback(HardwareType.CPU, UpdateCPU);
            _systemInfoService.UnregisterUpdatedCallback(HardwareType.GPU, UpdateGPU);
            _systemInfoService.UnregisterUpdatedCallback(HardwareType.Memory, UpdateMemory);
        }
        await Task.CompletedTask;
    }

    public void WidgetWindow_Closing()
    {
        _systemInfoService.UnregisterUpdatedCallback(HardwareType.CPU, UpdateCPU);
        _systemInfoService.UnregisterUpdatedCallback(HardwareType.GPU, UpdateGPU);
        _systemInfoService.UnregisterUpdatedCallback(HardwareType.Memory, UpdateMemory);
    }

    #endregion
}
