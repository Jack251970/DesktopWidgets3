namespace DesktopWidgets3.Contracts.Services;

public interface IPerformanceService
{
    (string UploadSpeed, string DownloadSpeed) GetNetworkSpeed();
}
