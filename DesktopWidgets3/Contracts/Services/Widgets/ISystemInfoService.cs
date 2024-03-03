namespace DesktopWidgets3.Contracts.Services.Widgets;

public interface ISystemInfoService
{
    bool OnBatterySaverChanged(bool batterySaver);

    void StartMonitor(WidgetType type);

    void StopMonitor(WidgetType type);

    (string UploadSpeed, string DownloadSpeed) GetNetworkSpeed(bool showBps);

    (string UploadSpeed, string DownloadSpeed) GetInitNetworkSpeed(bool showBps);

    (string CpuLoad, string CpuTempreture) GetCpuInfo(bool useCelsius);

    (string GpuLoad, string GpuTempreture) GetGpuInfo(bool useCelsius);

    public (string MemoryLoad, string MemoryUsedInfo) GetMemoryInfo();
}
