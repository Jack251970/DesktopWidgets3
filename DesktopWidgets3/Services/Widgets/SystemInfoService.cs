using System.Timers;

using Timer = System.Timers.Timer;

namespace DesktopWidgets3.Services.Widgets;

internal class SystemInfoService : ISystemInfoService
{
    private readonly IAppSettingsService _appSettingsService;

    private readonly HardwareMonitor hardwareMonitor = new();

    private readonly Timer sampleTimer = new();

    public SystemInfoService(IAppSettingsService appSettingsService)
    {
        _appSettingsService = appSettingsService;

        sampleTimer.AutoReset = true;
        sampleTimer.Enabled = false;
        sampleTimer.Interval = _appSettingsService.BatterySaver ? 1000 : 500;
        sampleTimer.Elapsed += SampleTimer_Elapsed;

        _appSettingsService.OnBatterySaverChanged += AppSettingsService_OnBatterySaverChanged;

        hardwareMonitor.EnabledChanged += HardwareMonitor_OnEnabledChanged;
    }

    private void SampleTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        hardwareMonitor.Update();
    }

    private void AppSettingsService_OnBatterySaverChanged(object? _, bool batterySaver)
    {
        var enabled = sampleTimer.Enabled;
        sampleTimer.Enabled = false;
        sampleTimer.Interval = batterySaver ? 1000 : 100;
        sampleTimer.Enabled = enabled;
    }

    #region hardware monitor

    private void HardwareMonitor_OnEnabledChanged(object? _, bool enabled)
    {
        if (hardwareMonitor.Enabled)
        {
            sampleTimer.Start();
        }
        else
        {
            sampleTimer.Stop();
        }
    }

    public void StartMonitor(WidgetType type)
    {
        switch (type)
        {
            case WidgetType.Network:
                hardwareMonitor.NetworkEnabled = true;
                break;
            case WidgetType.Performance:
                hardwareMonitor.CpuEnabled = true;
                hardwareMonitor.GpuEnabled = true;
                hardwareMonitor.MemoryEnabled = true;
                break;
            case WidgetType.Disk:
                hardwareMonitor.DiskEnabled = true;
                break;
        }
    }

    public void StopMonitor(WidgetType type)
    {
        switch (type)
        {
            case WidgetType.Network:
                hardwareMonitor.NetworkEnabled = false;
                break;
            case WidgetType.Performance:
                hardwareMonitor.CpuEnabled = false;
                hardwareMonitor.GpuEnabled = false;
                hardwareMonitor.MemoryEnabled = false;
                break;
            case WidgetType.Disk:
                hardwareMonitor.DiskEnabled = false;
                break;
        }
    }

    public void RegisterUpdatedCallback(HardwareType type, Action action)
    {
        switch (type)
        {
            case HardwareType.Network:
                hardwareMonitor.OnNetworkStatsUpdated += action;
                break;
            case HardwareType.CPU:
                hardwareMonitor.OnCpuStatsUpdated += action;
                break;
            case HardwareType.GPU:
                hardwareMonitor.OnGpuStatsUpdated += action;
                break;
            case HardwareType.Memory:
                hardwareMonitor.OnMemoryStatsUpdated += action;
                break;
            case HardwareType.Disk:
                hardwareMonitor.OnDiskStatsUpdated += action;
                break;
        }
    }

    public void UnregisterUpdatedCallback(HardwareType type, Action action)
    {
        switch (type)
        {
            case HardwareType.Network:
                hardwareMonitor.OnNetworkStatsUpdated -= action;
                break;
            case HardwareType.CPU:
                hardwareMonitor.OnCpuStatsUpdated -= action;
                break;
            case HardwareType.GPU:
                hardwareMonitor.OnGpuStatsUpdated -= action;
                break;
            case HardwareType.Memory:
                hardwareMonitor.OnMemoryStatsUpdated -= action;
                break;
            case HardwareType.Disk:
                hardwareMonitor.OnDiskStatsUpdated -= action;
                break;
        }
    }

    #endregion

    #region get stats

    public NetworkStats? GetNetworkStats()
    {
        return hardwareMonitor.GetNetworkStats();
    }

    public CPUStats? GetCPUStats()
    {
        return hardwareMonitor.GetCpuStats();
    }

    public GPUStats? GetGPUStats()
    {
        return hardwareMonitor.GetGpuStats();
    }

    public MemoryStats? GetMemoryStats()
    {
        return hardwareMonitor.GetMemoryStats();
    }

    public DiskStats? GetDiskStats()
    {
        return hardwareMonitor.GetDiskStats();
    }

    #endregion
}
