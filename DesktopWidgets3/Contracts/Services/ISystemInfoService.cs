using DesktopWidgets3.Models.Widget;

namespace DesktopWidgets3.Contracts.Services;

public interface ISystemInfoService
{
    (string UploadSpeed, string DownloadSpeed) GetNetworkSpeed(bool showBps);

    (string UploadSpeed, string DownloadSpeed) GetInitNetworkSpeed(bool showBps);

    void StartMonitor(WidgetType type);

    void StopMonitor(WidgetType type);
}
