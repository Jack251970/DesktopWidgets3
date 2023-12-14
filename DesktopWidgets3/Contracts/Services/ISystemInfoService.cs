namespace DesktopWidgets3.Contracts.Services;

public interface ISystemInfoService
{
    (string UploadSpeed, string DownloadSpeed) GetNetworkSpeed(bool showBps);
}
