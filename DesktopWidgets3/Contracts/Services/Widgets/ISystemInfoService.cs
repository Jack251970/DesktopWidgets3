namespace DesktopWidgets3.Contracts.Services.Widgets;

public interface ISystemInfoService
{
    void StartMonitor(WidgetType type);

    void StopMonitor(WidgetType type);

    NetworkSpeedInfo GetNetworkSpeed(bool showBps);

    NetworkSpeedInfo GetInitNetworkSpeed(bool showBps);

    (string CpuLoad, float CpuLoadValue, string CpuTempreture) GetCpuInfo(bool useCelsius);

    (string CpuLoad, float CpuLoadValue, string CpuTempreture) GetInitCpuInfo(bool useCelsius);

    (string GpuLoad, float GpuLoadValue, string GpuTempreture) GetGpuInfo(bool useCelsius);

    (string GpuLoad, float GpuLoadValue, string GpuTempreture) GetInitGpuInfo(bool useCelsius);

    (string MemoryLoad, float MemoryLoadValue, string MemoryUsedInfo) GetMemoryInfo();

    (string MemoryLoad, float MemoryLoadValue, string MemoryUsedInfo) GetInitMemoryInfo();

    DiskInfo GetDiskInfo();

    DiskInfo GetInitDiskInfo();
}
