// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HardwareInfoProvider.Helpers;

#pragma warning disable CA1822 // Mark members as static

internal sealed class DataManager(HardwareType type) : IDisposable
{
    private readonly SystemData _systemData = new();
    private readonly HardwareType _hardwareType = type;

    private static void GetMemoryData()
    {
        lock (SystemData.MemStats)
        {
            SystemData.MemStats.GetData();
        }
    }

    private static void GetNetworkData()
    {
        lock (SystemData.NetStats)
        {
            SystemData.NetStats.GetData();
        }
    }

    private static void GetGPUData()
    {
        lock (SystemData.GPUStats)
        {
            SystemData.GPUStats.GetData();
        }
    }

    private static void GetCPUData()
    {
        lock (SystemData.CpuStats)
        {
            SystemData.CpuStats.GetData();
        }
    }

    private static void GetDiskData()
    {
        lock (SystemData.DiskStats)
        {
            SystemData.DiskStats.GetData();
        }
    }

    public void Update()
    {
        switch (_hardwareType)
        {
            case HardwareType.CPU:
                {
                    // CPU
                    GetCPUData();
                    break;
                }

            case HardwareType.GPU:
                {
                    // gpu
                    GetGPUData();
                    break;
                }

            case HardwareType.Memory:
                {
                    // memory
                    GetMemoryData();
                    break;
                }

            case HardwareType.Network:
                {
                    // network
                    GetNetworkData();
                    break;
                }

            case HardwareType.Disk:
                {
                    // disk
                    GetDiskData();
                    break;
                }
        }
    }

    internal MemoryStats GetMemoryStats()
    {
        lock (SystemData.MemStats)
        {
            return SystemData.MemStats;
        }
    }

    internal NetworkStats GetNetworkStats()
    {
        lock (SystemData.NetStats)
        {
            return SystemData.NetStats;
        }
    }

    internal GPUStats GetGPUStats()
    {
        lock (SystemData.GPUStats)
        {
            return SystemData.GPUStats;
        }
    }

    internal CPUStats GetCPUStats()
    {
        lock (SystemData.CpuStats)
        {
            return SystemData.CpuStats;
        }
    }

    internal DiskStats GetDiskStats()
    {
        lock (SystemData.DiskStats)
        {
            return SystemData.DiskStats;
        }
    }

    public void Dispose()
    {
        _systemData.Dispose();
    }
}
