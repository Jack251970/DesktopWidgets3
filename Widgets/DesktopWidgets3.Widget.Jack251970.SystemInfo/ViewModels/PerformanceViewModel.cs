using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Timer = System.Timers.Timer;

namespace DesktopWidgets3.Widget.Jack251970.SystemInfo.ViewModels;

public partial class PerformanceViewModel : ObservableRecipient
{
    private static string ClassName => typeof(PerformanceViewModel).Name;

    #region view properties

    [ObservableProperty]
    private string _cpuLeftInfo = "--";

    [ObservableProperty]
    private string _cpuRightInfo = "--";

    [ObservableProperty]
    private double _cpuLoadValue = 0;

    [ObservableProperty]
    private string _gpuLeftInfo = "--";

    [ObservableProperty]
    private string _gpuRightInfo = "--";

    [ObservableProperty]
    private double _gpuLoadValue = 0;

    [ObservableProperty]
    private string _memoryLeftInfo = "--";

    [ObservableProperty]
    private string _memoryRightInfo = "--";

    [ObservableProperty]
    private double _memoryLoadValue = 0;

    #endregion

    #region settings

    private bool useCelsius = true;

    #endregion

    public string Id;

    private readonly DispatcherQueue _dispatcherQueue;

    private readonly HardwareInfoService _hardwareInfoService;

    private readonly Timer cpuUpdateTimer = new();
    private readonly Timer gpuUpdateTimer = new();
    private readonly Timer memoryUpdateTimer = new();

    public PerformanceViewModel(string widgetId, HardwareInfoService hardwareInfoService)
    {
        Id = widgetId;
        _dispatcherQueue = Main.WidgetInitContext.WidgetService.GetDispatcherQueue(Id);
        _hardwareInfoService = hardwareInfoService;
        InitializeAllTimers();
    }

    #region Timer Methods

    private void InitializeAllTimers()
    {
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

    public void StartAllTimers()
    {
        cpuUpdateTimer.Start();
        gpuUpdateTimer.Start();
        memoryUpdateTimer.Start();
    }

    public void StopAllTimers()
    {
        cpuUpdateTimer.Stop();
        gpuUpdateTimer.Stop();
        memoryUpdateTimer.Stop();
    }

    public void DisposeAllTimers()
    {
        cpuUpdateTimer.Dispose();
        gpuUpdateTimer.Dispose();
        memoryUpdateTimer.Dispose();
    }

    #endregion

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

            _dispatcherQueue.TryEnqueue(() =>
            {
                CpuLeftInfo = "CPU";
                CpuRightInfo = string.IsNullOrEmpty(cpuSpeed) ? FormatUtils.FormatPercentage(cpuUsage) : cpuSpeed;
                CpuLoadValue = cpuUsage * 100;
            });
        }
        catch (Exception e)
        {
            Main.WidgetInitContext.LogService.LogError(ClassName, e, "Error updating performance widget.");
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
            gpuName = gpuName.Replace("GPU", string.Empty).Trim();

            _dispatcherQueue.TryEnqueue(() =>
            {
                GpuLeftInfo = string.IsNullOrEmpty(gpuName) ? "GPU" : "GPU" + $" ({gpuName})";
                GpuRightInfo = gpuTemperature == 0 ? FormatUtils.FormatPercentage(gpuUsage) : FormatUtils.FormatTemperature(gpuTemperature, useCelsius);
                GpuLoadValue = gpuUsage * 100;
            });
        }
        catch (Exception e)
        {
            Main.WidgetInitContext.LogService.LogError(ClassName, e, "Error updating performance widget.");
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

            _dispatcherQueue.TryEnqueue(() =>
            {
                MemoryLeftInfo = Main.WidgetInitContext.LocalizationService.GetLocalizedString("Performance_Memory");
                MemoryRightInfo = allMem == 0 ? FormatUtils.FormatPercentage(memoryUsage) : memoryUsedInfo;
                MemoryLoadValue = memoryUsage * 100;
            });
        }
        catch (Exception e)
        {
            Main.WidgetInitContext.LogService.LogError(ClassName, e, "Error updating performance widget.");
        }
    }

    #endregion

    #region Settings Methods

    public void LoadSettings(BaseWidgetSettings settings)
    {
        if (settings is PerformanceSettings performanceSettings)
        {
            if (performanceSettings.UseCelsius != useCelsius)
            {
                useCelsius = performanceSettings.UseCelsius;
            }
        }
    }

    #endregion
}
