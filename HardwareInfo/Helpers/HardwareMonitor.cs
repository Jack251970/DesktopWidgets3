using System.Management;

namespace HardwareInfo.Helpers;

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
            { HardwareType.Network, new DataManager(HardwareType.Network) }
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

        // TODO
        /*if (DiskEnabled)
        {
            Hardwares[HardwareType.Disk].Update();
        }*/
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

    // TODO: Get it to DiskStas.
    public List<DiskInfoItem> GetDiskInfo()
    {
        List<DiskInfoItem> diskInfoItems = [];

        if (DiskEnabled)
        {
            string hardwareName;
            string hardwareIdentifier;

            float? diskUsed = null;
            float? diskTotal;

            if (diskInfoItems.Count == 0)
            {
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
                foreach (var disk in searcher.Get().Cast<ManagementObject>())
                {
                    hardwareName = disk["Model"].ToString()!;
                    hardwareIdentifier = disk["DeviceID"].ToString()!;
                    diskTotal = Convert.ToSingle(disk["Size"]);

                    List<PartitionInfoItem> partitionInfoItems = [];

                    var partitionSearcher = new ManagementObjectSearcher("ASSOCIATORS OF {Win32_DiskDrive.DeviceID='" + hardwareIdentifier + "'} WHERE AssocClass = Win32_DiskDriveToDiskPartition");
                    foreach (var partition in partitionSearcher.Get().Cast<ManagementObject>())
                    {
                        string partitionName = null!;
                        var partitionIdentifier = partition["DeviceID"].ToString()!;
                        float? partitionUsed = null;
                        float? partitionTotal = null;

                        var logicalDiskSearcher = new ManagementObjectSearcher("ASSOCIATORS OF {Win32_DiskPartition.DeviceID='" + partitionIdentifier + "'} WHERE AssocClass = Win32_LogicalDiskToPartition");
                        foreach (var logicalDisk in logicalDiskSearcher.Get().Cast<ManagementObject>())
                        {
                            partitionName = logicalDisk["Name"].ToString()!;
                            partitionTotal = Convert.ToSingle(logicalDisk["Size"]);
                            partitionUsed = partitionTotal - Convert.ToSingle(logicalDisk["FreeSpace"]);
                        }

                        partitionInfoItems.Add(new PartitionInfoItem
                        {
                            Name = partitionName,
                            Identifier = partitionIdentifier,
                            PartitionUsed = partitionUsed,
                            PartitionTotal = partitionTotal
                        });
                    }

                    diskInfoItems.Add(new DiskInfoItem
                    {
                        Name = hardwareName,
                        Identifier = hardwareIdentifier,
                        DiskUsed = diskUsed,
                        DiskTotal = diskTotal,
                        PartitionInfoItems = partitionInfoItems
                    });
                }
            }
        }

        return diskInfoItems;
    }

    public void Dispose()
    {
        foreach (var hardware in Hardwares)
        {
            hardware.Value.Dispose();
        }
    }
}
