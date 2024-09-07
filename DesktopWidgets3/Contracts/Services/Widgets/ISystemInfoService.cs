﻿namespace DesktopWidgets3.Contracts.Services.Widgets;

public interface ISystemInfoService
{
    void StartMonitor(WidgetType type);

    void StopMonitor(WidgetType type);

    void RegisterUpdatedCallback(HardwareType type, Action action);

    void UnregisterUpdatedCallback(HardwareType type, Action action);

    NetworkStats? GetNetworkStats();

    CPUStats? GetCPUStats();

    GPUStats? GetGPUStats();

    MemoryStats? GetMemoryStats();

    DiskStats? GetDiskStats();
}
