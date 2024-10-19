using Timer = System.Timers.Timer;

namespace DesktopWidgets3.Widget.Jack251970.SystemInfo.Models;

public partial class HardwareInfoService : IDisposable
{
    public Timer SampleTimer => sampleTimer;

    private readonly Timer sampleTimer = new() { AutoReset = true, Enabled = false };

    private readonly HardwareMonitor hardwareMonitor = new();

    public HardwareInfoService()
    {
        sampleTimer.Elapsed += (s, e) => hardwareMonitor.Update();
        hardwareMonitor.EnabledChanged += HardwareMonitor_OnEnabledChanged;
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

    public void StartMonitor(HardwareType type)
    {
        switch (type)
        {
            case HardwareType.Network:
                hardwareMonitor.NetworkEnabled = true;
                break;
            case HardwareType.CPU:
                hardwareMonitor.CpuEnabled = true;
                break;
            case HardwareType.GPU:
                hardwareMonitor.GpuEnabled = true;
                break;
            case HardwareType.Memory:
                hardwareMonitor.MemoryEnabled = true;
                break;
            case HardwareType.Disk:
                hardwareMonitor.DiskEnabled = true;
                break;
        }
    }

    public void StopMonitor(HardwareType type)
    {
        switch (type)
        {
            case HardwareType.Network:
                hardwareMonitor.NetworkEnabled = false;
                break;
            case HardwareType.CPU:
                hardwareMonitor.CpuEnabled = false;
                break;
            case HardwareType.GPU:
                hardwareMonitor.GpuEnabled = false;
                break;
            case HardwareType.Memory:
                hardwareMonitor.MemoryEnabled = false;
                break;
            case HardwareType.Disk:
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

    #region IDisposable

    private bool disposed;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                hardwareMonitor.Dispose();
                sampleTimer.Dispose();
            }

            disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
