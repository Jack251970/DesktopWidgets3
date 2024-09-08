// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management;
using Microsoft.Management.Infrastructure;

namespace HardwareInfo.Helpers;

public sealed class DiskStats : IDisposable
{
    // Disk counters
    private Dictionary<string, Data> DiskUsages { get; set; } = [];

    public sealed class Data
    {
        public string Name { get; set; } = string.Empty;

        public string DeviceId { get; set; } = string.Empty;

        public ulong Size { get; set; }

        public List<PartitionData> PartitionDatas { get; set; } = [];
    }

    public sealed class PartitionData
    {
        public string Name { get; set; } = string.Empty;

        public string DeviceId { get; set; } = string.Empty;

        public ulong Size { get; set; }

        public ulong FreeSpace { get; set; }

        public float Usage => Size > 0 ? (float)(Size - FreeSpace) / Size : 0;
    }

    public DiskStats() {}

    public void LoadDisks()
    {
        var diskUsages = new Dictionary<string, Data>();

        using var session = CimSession.Create(null);
        var disks = session.QueryInstances("root/cimv2", "WQL", "select * from Win32_DiskDrive");
        foreach (var obj in disks)
        {
            var diskName = (string)obj.CimInstanceProperties["Model"].Value;
            var diskDeviceId = (string)obj.CimInstanceProperties["DeviceID"].Value;
            var diskSize = (ulong)(obj.CimInstanceProperties["Size"].Value ?? (ulong)0);
            var paritionDatas = new List<PartitionData>();

            // Use management object searcher to get partitions because there are some issues with CimSession
            var partitionSearcher = new ManagementObjectSearcher($"ASSOCIATORS OF {{Win32_DiskDrive.DeviceID='{diskDeviceId}'}} WHERE AssocClass = Win32_DiskDriveToDiskPartition");
            var partitions = partitionSearcher.Get().Cast<ManagementObject>();
            foreach (var partition in partitions)
            {
                var partitionDeviceId = (string)partition["DeviceID"];

                var logicalDiskSearcher = new ManagementObjectSearcher($"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partitionDeviceId}'}} WHERE AssocClass = Win32_LogicalDiskToPartition");
                var logicalDisks = logicalDiskSearcher.Get().Cast<ManagementObject>();
                foreach (var logicalDisk in logicalDisks)
                {
                    var partitionName = (string)logicalDisk["Name"];
                    var size = (ulong)logicalDisk["Size"];
                    var freeSpace = (ulong)logicalDisk["FreeSpace"];

                    paritionDatas.Add(new PartitionData
                    {
                        Name = partitionName,
                        DeviceId = partitionDeviceId,
                        Size = size,
                        FreeSpace = freeSpace
                    });
                }
            }
            /*var partitions = session.QueryInstances("root/cimv2", "WQL", $"ASSOCIATORS OF {{Win32_DiskDrive.DeviceID='{diskDeviceId}'}} WHERE AssocClass = Win32_DiskDriveToDiskPartition");
            foreach (var partition in partitions)
            {
                var partitionDeviceId = (string)partition.CimInstanceProperties["DeviceID"].Value;

                var logicalDisks = session.QueryInstances("root/cimv2", "WQL", $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partitionDeviceId}'}} WHERE AssocClass = Win32_LogicalDiskToPartition");
                foreach(var logicalDisk in logicalDisks)
                {
                    var partitionName = (string)logicalDisk.CimInstanceProperties["Name"].Value;
                    var size = (ulong)(logicalDisk.CimInstanceProperties["Size"].Value ?? (ulong)0);
                    var freeSpace = (ulong)(logicalDisk.CimInstanceProperties["FreeSpace"].Value ?? (ulong)0);

                    _stats[i].PartitionDatas.Add(new PartitionData
                    {
                        Name = partitionName,
                        DeviceId = partitionDeviceId,
                        Size = size,
                        FreeSpace = freeSpace
                    });
                }
            }*/

            diskUsages.Add(diskDeviceId, new Data() { Name = diskName, DeviceId = diskDeviceId, Size = diskSize, PartitionDatas = paritionDatas });
        }

        DiskUsages = diskUsages;
    }

    public void GetData()
    {
        LoadDisks();
    }

    public int GetDiskCount()
    {
        return DiskUsages.Count;
    }

    public string GetDiskDeviceId(int diskActiveIndex)
    {
        if (DiskUsages.Count <= diskActiveIndex)
        {
            return string.Empty;
        }

        return DiskUsages.ElementAt(diskActiveIndex).Key;
    }

    public Data GetDiskUsage(int diskActiveIndex)
    {
        if (DiskUsages.Count <= diskActiveIndex)
        {
            return new Data();
        }

        var currDiskDeviceId = DiskUsages.ElementAt(diskActiveIndex).Key;
        if (!DiskUsages.TryGetValue(currDiskDeviceId, out var value))
        {
            return new Data();
        }

        return value;
    }

    public int GetPrevDiskIndex(int diskActiveIndex)
    {
        if (DiskUsages.Count == 0)
        {
            return 0;
        }

        if (diskActiveIndex == 0)
        {
            return DiskUsages.Count - 1;
        }

        return diskActiveIndex - 1;
    }

    public int GetNextDiskIndex(int diskActiveIndex)
    {
        if (DiskUsages.Count == 0)
        {
            return 0;
        }

        if (diskActiveIndex == DiskUsages.Count - 1)
        {
            return 0;
        }

        return diskActiveIndex + 1;
    }

    public void Dispose()
    {
        
    }
}
