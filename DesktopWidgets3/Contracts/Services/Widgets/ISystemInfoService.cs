using HardwareInfo.Helpers;

namespace DesktopWidgets3.Contracts.Services.Widgets;

public interface ISystemInfoService
{
    void StartMonitor(WidgetType type);

    void StopMonitor(WidgetType type);

    void RegisterUpdatedCallback(HardwareType type, Action action);

    void UnregisterUpdatedCallback(HardwareType type, Action action);

    NetworkSpeedInfo GetNetworkSpeed(bool useBps);

    (string CpuLoad, float CpuLoadValue, string CpuSpeed) GetCpuInfo();

    (string GpuName, string GpuLoad, float GpuLoadValue, string GpuInfo) GetGpuInfo(bool useCelsius);

    (string MemoryLoad, float MemoryLoadValue, string MemoryUsedInfo) GetMemoryInfo();

    DiskInfo GetDiskInfo();
}
