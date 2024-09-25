using System;
using System.Timers;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;

namespace DesktopWidgets3.Widget.Jack251970.Performance.ViewModels;

public partial class PerformanceViewModel : BaseWidgetViewModel, IWidgetUpdate, IWidgetWindowClose
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

    private readonly HardwareInfoService _hardwareInfoService;

    private readonly Timer cpuUpdateTimer = new();
    private readonly Timer gpuUpdateTimer = new();
    private readonly Timer memoryUpdateTimer = new();

    public PerformanceViewModel(HardwareInfoService hardwareInfoService)
    {
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

    #region Update Methods

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
                CpuLeftInfo = "CPU";
                CpuRightInfo = string.IsNullOrEmpty(cpuSpeed) ? FormatUtils.FormatPercentage(cpuUsage) : cpuSpeed;
                CpuLoadValue = cpuUsage * 100;
            });
        }
        catch (Exception e)
        {
            Main.Context.LogService.LogError(ClassName, e, "Error updating performance widget.");
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

            // remove unnecessary strings from GPU name.
            gpuName = gpuName.Replace("(TM)", string.Empty).Replace("(R)", string.Empty).Replace("GPU", string.Empty).Trim();

            DispatcherQueue.TryEnqueue(() =>
            {
                GpuLeftInfo = string.IsNullOrEmpty(gpuName) ? "GPU" : "GPU" + $" ({gpuName})";
                GpuRightInfo = gpuTemperature == 0 ? FormatUtils.FormatPercentage(gpuUsage) : FormatUtils.FormatTemperature(gpuTemperature, useCelsius);
                GpuLoadValue = gpuUsage * 100;
            });
        }
        catch (Exception e)
        {
            Main.Context.LogService.LogError(ClassName, e, "Error updating performance widget.");
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
                MemoryLeftInfo = Main.Context.LocalizationService.GetLocalizedString("Memory");
                MemoryRightInfo = allMem == 0 ? FormatUtils.FormatPercentage(memoryUsage) : memoryUsedInfo;
                MemoryLoadValue = memoryUsage * 100;
            });
        }
        catch (Exception e)
        {
            Main.Context.LogService.LogError(ClassName, e, "Error updating performance widget.");
        }
    }

    #endregion

    #region Abstract Methods

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

    #region IWidgetUpdate

    public void EnableUpdate(bool enable)
    {
        cpuUpdateTimer.Enabled = enable;
        gpuUpdateTimer.Enabled = enable;
        memoryUpdateTimer.Enabled = enable;
    }

    #endregion

    #region IWidgetWindowClose

    public void WidgetWindowClosing()
    {
        cpuUpdateTimer.Dispose();
        gpuUpdateTimer.Dispose();
        memoryUpdateTimer.Dispose();
    }

    #endregion
}
