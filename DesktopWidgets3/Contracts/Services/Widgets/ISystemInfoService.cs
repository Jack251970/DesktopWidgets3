namespace DesktopWidgets3.Contracts.Services.Widgets;

public interface ISystemInfoService
{
    bool OnBatterySaverChanged(bool batterySaver);

    void StartMonitor(WidgetType type);

    void StopMonitor(WidgetType type);

    (string UploadSpeed, string DownloadSpeed) GetNetworkSpeed(bool showBps);

    (string UploadSpeed, string DownloadSpeed) GetInitNetworkSpeed(bool showBps);
}
