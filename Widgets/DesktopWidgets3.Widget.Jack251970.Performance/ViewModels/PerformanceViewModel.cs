using System;
using System.Threading.Tasks;
using System.Timers;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;

namespace DesktopWidgets3.Widget.Jack251970.Performance.ViewModels;

public partial class PerformanceViewModel : BaseWidgetViewModel, IWidgetUpdate, IWidgetClose
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

    private readonly Timer cpuUpdateTimer = new();
    private readonly Timer gpuUpdateTimer = new();
    private readonly Timer memoryUpdateTimer = new();

    public PerformanceViewModel(WidgetInitContext context, HardwareInfoService hardwareInfoService)
    {
        Context = context;

        _hardwareInfoService = hardwareInfoService;

        InitializeTimer(cpuUpdateTimer, UpdateCPU);
        InitializeTimer(gpuUpdateTimer, UpdateGPU);
        InitializeTimer(memoryUpdateTimer, UpdateMemory);
    }

    private static void InitializeTimer(Timer timer, Action action)
    {
        timer.AutoReset = true;
        timer.Enabled = false;
        timer.Interval = 1000;
        timer.Elapsed += (s, e) => action();
    }

    #region update methods

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
                CpuLeftInfo = "Cpu";
                CpuRightInfo = string.IsNullOrEmpty(cpuSpeed) ? FormatUtils.FormatPercentage(cpuUsage) : cpuSpeed;
                CpuLoadValue = cpuUsage * 100;
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
                GpuLeftInfo = string.IsNullOrEmpty(gpuName) ? "Gpu" : "Gpu" + $" ({gpuName})";
                GpuRightInfo = gpuTemperature == 0 ? FormatUtils.FormatPercentage(gpuUsage) : FormatUtils.FormatTemperature(gpuTemperature, useCelsius);
                GpuLoadValue = gpuUsage * 100;
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
                MemoryLeftInfo = "Memory";
                MemoryRightInfo = allMem == 0 ? FormatUtils.FormatPercentage(memoryUsage) : memoryUsedInfo;
                MemoryLoadValue = memoryUsage * 100;
            });
        }
        catch (Exception e)
        {
            Context.API.LogError(ClassName, e, "Error updating performance widget.");
        }
    }

    #endregion

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

            cpuUpdateTimer.Start();
            gpuUpdateTimer.Start();
            memoryUpdateTimer.Start();
        }
    }

    #endregion

    #region widget update

    public async Task EnableUpdate(bool enable)
    {
        cpuUpdateTimer.Enabled = enable;
        gpuUpdateTimer.Enabled = enable;
        memoryUpdateTimer.Enabled = enable;

        await Task.CompletedTask;
    }

    #endregion

    #region widget closing

    public void OnWidgetClose()
    {
        cpuUpdateTimer.Dispose();
        gpuUpdateTimer.Dispose();
        memoryUpdateTimer.Dispose();
    }

    #endregion
}
