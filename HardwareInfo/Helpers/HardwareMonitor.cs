﻿namespace HardwareInfo.Helpers;

/// <summary>
/// Hardware monitor.
/// </summary>
public class HardwareMonitor: IDisposable
{
    public bool Enabled => NetworkEnabled || CpuEnabled || GpuEnabled || MemoryEnabled || DiskEnabled;

    public event EventHandler<bool>? EnabledChanged;

    public bool networkEnabled;
    public bool NetworkEnabled
    {
        get => networkEnabled;
        set
        {
            var enabledBefore = Enabled;
            networkEnabled = value;
            var enabledAfter = Enabled;
            if (enabledBefore != enabledAfter)
            {
                EnabledChanged?.Invoke(this, enabledAfter);
            }
        }
    }

    private bool cpuEnabled;
    public bool CpuEnabled
    {
        get => cpuEnabled;
        set
        {
            var enabledBefore = Enabled;
            cpuEnabled = value;
            var enabledAfter = Enabled;
            if (enabledBefore != enabledAfter)
            {
                EnabledChanged?.Invoke(this, enabledAfter);
            }
        }
    }

    public bool gpuEnabled;
    public bool GpuEnabled
    {
        get => gpuEnabled;
        set
        {
            var enabledBefore = Enabled;
            gpuEnabled = value;
            var enabledAfter = Enabled;
            if (enabledBefore != enabledAfter)
            {
                EnabledChanged?.Invoke(this, enabledAfter);
            }
        }
    }

    public bool memoryEnabled;
    public bool MemoryEnabled
    {
        get => memoryEnabled;
        set
        {
            var enabledBefore = Enabled;
            memoryEnabled = value;
            var enabledAfter = Enabled;
            if (enabledBefore != enabledAfter)
            {
                EnabledChanged?.Invoke(this, enabledAfter);
            }
        }
    }

    public bool diskEnabled;
    public bool DiskEnabled
    {
        get => diskEnabled;
        set
        {
            var enabledBefore = Enabled;
            diskEnabled = value;
            var enabledAfter = Enabled;
            if (enabledBefore != enabledAfter)
            {
                EnabledChanged?.Invoke(this, enabledAfter);
            }
        }
    }

    private readonly Dictionary<HardwareType, DataManager> Hardwares;

    public HardwareMonitor()
    {
        Hardwares = new Dictionary<HardwareType, DataManager>
        {
            { HardwareType.CPU, new DataManager(HardwareType.CPU) },
            { HardwareType.GPU, new DataManager(HardwareType.GPU) },
            { HardwareType.Memory, new DataManager(HardwareType.Memory) },
            { HardwareType.Network, new DataManager(HardwareType.Network) },
            { HardwareType.Disk, new DataManager(HardwareType.Disk) }
        };
    }

    public void Update()
    {
        if (CpuEnabled)
        {
            Hardwares[HardwareType.CPU].Update();
        }

        if (GpuEnabled)
        {
            Hardwares[HardwareType.GPU].Update();
        }

        if (MemoryEnabled)
        {
            Hardwares[HardwareType.Memory].Update();
        }

        if (NetworkEnabled)
        {
            Hardwares[HardwareType.Network].Update();
        }

        if (DiskEnabled)
        {
            Hardwares[HardwareType.Disk].Update();
        }
    }

    public CPUStats? GetCpuStats()
    {
        if (!CpuEnabled)
        {
            return null;
        }

        return Hardwares[HardwareType.CPU].GetCPUStats();
    }

    public GPUStats? GetGpuStats()
    {
        if (!GpuEnabled)
        {
            return null;
        }

        return Hardwares[HardwareType.GPU].GetGPUStats();
    }

    public MemoryStats? GetMemoryStats()
    {
        if (!MemoryEnabled)
        {
            return null;
        }

        return Hardwares[HardwareType.Memory].GetMemoryStats();
    }

    public NetworkStats? GetNetworkStats()
    {
        if (!NetworkEnabled)
        {
            return null;
        }

        return Hardwares[HardwareType.Network].GetNetworkStats();
    }

    public DiskStats? GetDiskInfo()
    {
        if (!DiskEnabled)
        {
            return null;
        }

        return Hardwares[HardwareType.Disk].GetDiskStats();
    }

    public void Dispose()
    {
        foreach (var hardware in Hardwares)
        {
            hardware.Value.Dispose();
        }
    }
}
