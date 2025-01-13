﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HardwareInfoProvider.Helpers;

internal sealed partial class SystemData : IDisposable
{
    public static MemoryStats MemStats { get; set; } = new MemoryStats();

    public static NetworkStats NetStats { get; set; } = new NetworkStats();

    public static GPUStats GPUStats { get; set; } = new GPUStats();

    // TODO: Exception when starting DevHome???
    public static CPUStats CpuStats { get; set; } = new CPUStats();

    public static DiskStats DiskStats { get; set; } = new DiskStats();

    public SystemData()
    {
    }

    public void Dispose()
    {
    }
}
