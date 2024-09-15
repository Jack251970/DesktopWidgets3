using System.Timers;
using System;

namespace DesktopWidgets3.Widget.Jack251970.Network.Models;

public class HardwareInfoService : IDisposable
{
    private readonly WidgetInitContext Context;

    private readonly HardwareMonitor hardwareMonitor = new();

    private readonly Timer sampleTimer = new();

    public HardwareInfoService(WidgetInitContext context)
    {
        Context = context;
        Context.SettingsService.OnBatterySaverChanged += OnBatterySaverChanged;

        sampleTimer.AutoReset = true;
        sampleTimer.Enabled = false;
        sampleTimer.Interval = context.SettingsService.BatterySaver ? 1000 : 200;
        sampleTimer.Elapsed += (s, e) => hardwareMonitor.Update();

        hardwareMonitor.EnabledChanged += HardwareMonitor_OnEnabledChanged;
    }

    public void OnBatterySaverChanged(bool enable)
    {
        var enabled = sampleTimer.Enabled;
        sampleTimer.Enabled = false;
        sampleTimer.Interval = enable ? 1000 : 200;
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
                Context.SettingsService.OnBatterySaverChanged -= OnBatterySaverChanged;
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
