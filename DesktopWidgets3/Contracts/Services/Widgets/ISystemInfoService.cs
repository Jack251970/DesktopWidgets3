namespace DesktopWidgets3.Contracts.Services.Widgets;

public interface ISystemInfoService
{
    bool OnBatterySaverChanged(bool batterySaver);

    void StartMonitor(WidgetType type);

    void StopMonitor(WidgetType type);

    (string UploadSpeed, string DownloadSpeed) GetNetworkSpeed(bool showBps);

    (string UploadSpeed, string DownloadSpeed) GetInitNetworkSpeed(bool showBps);

    (string CpuLoad, string CpuTempreture) GetCpuInfo(bool useCelsius);

    (string CpuLoad, string CpuTempreture) GetInitCpuInfo(bool useCelsius);

    (string GpuLoad, string GpuTempreture) GetGpuInfo(bool useCelsius);

    (string GpuLoad, string GpuTempreture) GetInitGpuInfo(bool useCelsius);

    (string MemoryLoad, string MemoryUsedInfo) GetMemoryInfo();

    (string MemoryLoad, string MemoryUsedInfo) GetInitMemoryInfo();
}
