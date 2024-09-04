namespace DesktopWidgets3.Contracts.Services.Widgets;

public interface ISystemInfoService
{
    void StartMonitor(WidgetType type);

    void StopMonitor(WidgetType type);

    NetworkSpeedInfo GetNetworkSpeed(bool useBps);

    NetworkSpeedInfo GetInitNetworkSpeed(bool useBps);

    (string CpuLoad, float CpuLoadValue, string CpuSpeed) GetCpuInfo();

    (string CpuLoad, float CpuLoadValue, string CpuSpeed) GetInitCpuInfo();

    (string GpuName, string GpuLoad, float GpuLoadValue, string GpuInfo) GetGpuInfo(bool useCelsius);

    (string GpuName, string GpuLoad, float GpuLoadValue, string GpuInfo) GetInitGpuInfo(bool useCelsius);

    (string MemoryLoad, float MemoryLoadValue, string MemoryUsedInfo) GetMemoryInfo();

    (string MemoryLoad, float MemoryLoadValue, string MemoryUsedInfo) GetInitMemoryInfo();

    DiskInfo GetDiskInfo();
}
