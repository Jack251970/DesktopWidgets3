using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;

namespace DesktopWidgets3.Widget.Jack251970.Performance.ViewModels;

public partial class PerformanceViewModel : BaseWidgetViewModel, IWidgetUpdate, IWidgetClosing
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

    private readonly WidgetInitContext Context;

    private readonly HardwareInfoService _hardwareInfoService;

    private bool cpuUpdating = false;
    private bool gpuUpdating = false;
    private bool memoryUpdating = false;

    public PerformanceViewModel(WidgetInitContext context, HardwareInfoService hardwareInfoService)
    {
        Context = context;

        _hardwareInfoService = hardwareInfoService;
        _hardwareInfoService.RegisterUpdatedCallback(HardwareType.CPU, UpdateCPU);
        _hardwareInfoService.RegisterUpdatedCallback(HardwareType.GPU, UpdateGPU);
        _hardwareInfoService.RegisterUpdatedCallback(HardwareType.Memory, UpdateMemory);
    }

    private void UpdateCPU()
    {
        try
        {
            var cpuStats = _hardwareInfoService.GetCPUStats();

            if (cpuStats == null)
            {
                return;
            }

            var cpuUsage = cpuStats.CpuUsage;
            var cpuSpeed = FormatUtils.FormatCpuSpeed(cpuStats.CpuSpeed);

            DispatcherQueue.TryEnqueue(() =>
            {
                if (cpuUpdating)
                {
                    return;
                }

                cpuUpdating = true;

                CpuLeftInfo = "Cpu";
                CpuRightInfo = string.IsNullOrEmpty(cpuSpeed) ? FormatUtils.FormatPercentage(cpuUsage) : cpuSpeed;
                CpuLoadValue = cpuUsage * 100;

                cpuUpdating = false;
            });
        }
        catch (Exception e)
        {
            Context.API.LogError(ClassName, e, "Error updating performance widget.");
        }
    }

    private void UpdateGPU()
    {
        try
        {
            var gpuStats = _hardwareInfoService.GetGPUStats();

            if (gpuStats == null)
            {
                return;
            }

            // TODO: Add actite index support.
            var _gpuActiveIndex = 0;
            var gpuName = gpuStats.GetGPUName(_gpuActiveIndex);
            var gpuUsage = gpuStats.GetGPUUsage(_gpuActiveIndex);
            var gpuTemperature = gpuStats.GetGPUTemperature(_gpuActiveIndex);

            DispatcherQueue.TryEnqueue(() =>
            {
                if (gpuUpdating)
                {
                    return;
                }

                gpuUpdating = true;

                GpuLeftInfo = string.IsNullOrEmpty(gpuName) ? "Gpu" : "Gpu" + $" ({gpuName})";
                GpuRightInfo = gpuTemperature == 0 ? FormatUtils.FormatPercentage(gpuUsage) : FormatUtils.FormatTemperature(gpuTemperature, useCelsius);
                GpuLoadValue = gpuUsage * 100;

                gpuUpdating = false;
            });
        }
        catch (Exception e)
        {
            Context.API.LogError(ClassName, e, "Error updating performance widget.");
        }
    }

    private void UpdateMemory()
    {
        try
        {
            var memoryStats = _hardwareInfoService.GetMemoryStats();

            if (memoryStats == null)
            {
                return;
            }

            var usedMem = memoryStats.UsedMem;
            var memoryUsage = memoryStats.MemUsage;
            var allMem = memoryStats.AllMem;
            var memoryUsedInfo = FormatUtils.FormatUsedInfoByte(usedMem, allMem);

            DispatcherQueue.TryEnqueue(() =>
            {
                if (memoryUpdating)
                {
                    return;
                }

                memoryUpdating = true;

                MemoryLeftInfo = "Memory";
                MemoryRightInfo = allMem == 0 ? FormatUtils.FormatPercentage(memoryUsage) : memoryUsedInfo;
                MemoryLoadValue = memoryUsage * 100;

                memoryUpdating = false;
            });
        }
        catch (Exception e)
        {
            Context.API.LogError(ClassName, e, "Error updating performance widget.");
        }
    }

    #region abstract methods

    protected override void LoadSettings(BaseWidgetSettings settings, bool initialized)
    {
        // initialize or update widget from settings
        if (settings is PerformanceSettings performanceSettings)
        {
            if (performanceSettings.UseCelsius != useCelsius)
            {
                useCelsius = performanceSettings.UseCelsius;
            }
        }

        // initialize widget
        if (initialized)
        {
            CpuLeftInfo = "--";
            CpuRightInfo = "--";
            GpuLeftInfo = "--";
            GpuRightInfo = "--";
            MemoryLeftInfo = "--";
            MemoryRightInfo = "--";
        }
    }

    #endregion

    #region widget update

    public async Task EnableUpdate(bool enable)
    {
        if (enable)
        {
            _hardwareInfoService.RegisterUpdatedCallback(HardwareType.CPU, UpdateCPU);
            _hardwareInfoService.RegisterUpdatedCallback(HardwareType.GPU, UpdateGPU);
            _hardwareInfoService.RegisterUpdatedCallback(HardwareType.Memory, UpdateMemory);
        }
        else
        {
            _hardwareInfoService.UnregisterUpdatedCallback(HardwareType.CPU, UpdateCPU);
            _hardwareInfoService.UnregisterUpdatedCallback(HardwareType.GPU, UpdateGPU);
            _hardwareInfoService.UnregisterUpdatedCallback(HardwareType.Memory, UpdateMemory);
        }

        await Task.CompletedTask;
    }

    #endregion

    #region widget closing

    public void WidgetWindow_Closing()
    {
        _hardwareInfoService.UnregisterUpdatedCallback(HardwareType.CPU, UpdateCPU);
        _hardwareInfoService.UnregisterUpdatedCallback(HardwareType.GPU, UpdateGPU);
        _hardwareInfoService.UnregisterUpdatedCallback(HardwareType.Memory, UpdateMemory);
    }

    #endregion
}
