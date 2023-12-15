using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Models.Widget;
using Timer = System.Timers.Timer;

namespace DesktopWidgets3.Services;

public class SystemInfoService : ISystemInfoService
{
    private readonly IAppSettingsService _appSettingsService;

    private readonly Timer sampleTimer = new();

    public SystemInfoService(IAppSettingsService appSettingsService)
    {
        _appSettingsService = appSettingsService;

        sampleTimer.AutoReset = true;
        sampleTimer.Enabled = false;
        sampleTimer.Interval = _appSettingsService.BatterySaver ? 1000 : 100;
    }

    private bool IsMonitorOpen => _isNetworkMonitorOpen;

    public void StartMonitor(WidgetType type)
    {
        switch (type)
        {
            case WidgetType.Network:
                IsNetworkMonitorOpen = true;
                break;
        }
    }

    public void StopMonitor(WidgetType type)
    {
        switch (type)
        {
            case WidgetType.Network:
                IsNetworkMonitorOpen = false;
                break;
        }
    }

    #region network speed

    private readonly NetWorkMonitor netWorkMonitor = new();

    private bool _isNetworkMonitorOpen;
    private bool IsNetworkMonitorOpen
    {
        get => _isNetworkMonitorOpen;
        set
        {
            if (value != _isNetworkMonitorOpen)
            {
                _isNetworkMonitorOpen = value;

                if (value)
                {
                    netWorkMonitor.Open();
                    sampleTimer.Elapsed += (s, e) => UpdateNetworkSpeed();
                    sampleTimer.Start();
                }
                else
                {
                    netWorkMonitor.Close();
                    sampleTimer.Elapsed -= (s, e) => UpdateNetworkSpeed();
                    if (!IsMonitorOpen)
                    {
                        sampleTimer.Stop();
                    }
                }
            }
        }
    }

    public (string UploadSpeed, string DownloadSpeed) GetNetworkSpeed(bool showBps)
    {
        var (uploadSpeed, downloadSpeed) = netWorkMonitor.GetNetworkSpeed();

        return (FormatSpeed(uploadSpeed, showBps), FormatSpeed(downloadSpeed, showBps));  
    }

    public (string UploadSpeed, string DownloadSpeed) GetInitNetworkSpeed(bool showBps)
    {
        return (FormatSpeed(0, showBps), FormatSpeed(0, showBps));
    }

    private void UpdateNetworkSpeed()
    {
        netWorkMonitor.Update();
    }

    private static string FormatSpeed(float bytes, bool showBps)
    {
        var unit = showBps ? "bps" : "B/s";
        if (showBps)
        {
            bytes *= 8;
        }

        const ulong kilobyte = 1024;
        const ulong megabyte = 1024 * kilobyte;
        const ulong gigabyte = 1024 * megabyte;

        if (bytes < kilobyte)
        {
            return $"{bytes:F2} {unit}";
        }
        else if (bytes < megabyte)
        {
            return $"{bytes / kilobyte:F2} K{unit}";
        }
        else if (bytes < gigabyte)
        {
            return $"{bytes / megabyte:F2} M{unit}";
        }
        else
        {
            return $"{bytes / gigabyte:F2} G{unit}";
        }
    }

    #endregion
}
