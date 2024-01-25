using DesktopWidgets3.Models.Widget;
using Timer = System.Timers.Timer;

namespace DesktopWidgets3.Services;

public class SystemInfoService : ISystemInfoService
{
    private readonly IAppSettingsService _appSettingsService;

    private readonly HardwareMonitor hardwareMonitor = new();
    private readonly Timer sampleTimer = new();

    private bool IsMonitorOpen => _isNetworkMonitorOpen;

    public SystemInfoService(IAppSettingsService appSettingsService)
    {
        _appSettingsService = appSettingsService;

        sampleTimer.AutoReset = true;
        sampleTimer.Enabled = false;
        OnBatterySaverChanged(_appSettingsService.BatterySaver);
    }

    public bool OnBatterySaverChanged(bool batterySaver)
    {
        sampleTimer.Interval = batterySaver ? 1000 : 100;
        return true;
    }

    public void StartMonitor(WidgetType type)
    {
        switch (type)
        {
            case WidgetType.Network:
                SetNetworkMonitor(true);
                break;
        }
    }

    public void StopMonitor(WidgetType type)
    {
        switch (type)
        {
            case WidgetType.Network:
                SetNetworkMonitor(false);
                break;
        }
    }

    #region network speed

    private bool _isNetworkMonitorOpen;
    private void SetNetworkMonitor(bool enabled)
    {
        if (enabled != _isNetworkMonitorOpen)
        {
            _isNetworkMonitorOpen = enabled;

            if (enabled)
            {
                hardwareMonitor.NetworkEnabled = true;
                hardwareMonitor.Open();
                sampleTimer.Start();
                sampleTimer.Elapsed += (s, e) => UpdateNetworkSpeed();
            }
            else
            {
                hardwareMonitor.NetworkEnabled = false;
                if (!IsMonitorOpen)
                {
                    hardwareMonitor.Close();
                    sampleTimer.Stop();
                }
                sampleTimer.Elapsed -= (s, e) => UpdateNetworkSpeed();
            }
        }
    }

    public (string UploadSpeed, string DownloadSpeed) GetNetworkSpeed(bool showBps)
    {
        var (uploadSpeed, downloadSpeed) = hardwareMonitor.GetNetworkInfo();

        return (FormatSpeed(uploadSpeed, showBps), FormatSpeed(downloadSpeed, showBps));  
    }

    public (string UploadSpeed, string DownloadSpeed) GetInitNetworkSpeed(bool showBps)
    {
        return (FormatSpeed(0, showBps), FormatSpeed(0, showBps));
    }

    private void UpdateNetworkSpeed()
    {
        hardwareMonitor.Update();
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
