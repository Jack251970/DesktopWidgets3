using HardwareInfo.Helpers;

using Timer = System.Timers.Timer;

namespace DesktopWidgets3.Services.Widgets;

internal class SystemInfoService : ISystemInfoService
{
    private readonly IAppSettingsService _appSettingsService;

    public SystemInfoService(IAppSettingsService appSettingsService)
    {
        _appSettingsService = appSettingsService;

        sampleTimer.AutoReset = true;
        sampleTimer.Enabled = false;

        OnBatterySaverChanged(_appSettingsService.BatterySaver);
    }

    // Callback for app settings when BatterySaver property changed.
    public bool OnBatterySaverChanged(bool batterySaver)
    {
        sampleTimer.Interval = batterySaver ? 1000 : 100;
        return true;
    }

    #region monitor & sample timer

    private readonly HardwareMonitor hardwareMonitor = new();

    private readonly Timer sampleTimer = new();

    private bool IsMonitorOpen => _isNetworkMonitorOpen || _isCpuGpuMemoryMonitorOpen;

    public void StartMonitor(WidgetType type)
    {
        switch (type)
        {
            case WidgetType.Network:
                SetNetworkMonitor(true);
                break;
            case WidgetType.Performance:
                SetCpuGpuMemoryMonitor(true);
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
            case WidgetType.Performance:
                SetCpuGpuMemoryMonitor(false);
                break;
        }
    }

    private void SetMonitorTimer(bool lastIsMonitorOpen, bool enabled)
    {
        // Check if need to change monitor status
        if (lastIsMonitorOpen == IsMonitorOpen)
        {
            return;
        }

        if (enabled)
        {
            hardwareMonitor.Open();
            sampleTimer.Start();
            sampleTimer.Elapsed -= UpdateMonitor;
            sampleTimer.Elapsed += UpdateMonitor;
        }
        else
        {
            hardwareMonitor.Close();
            sampleTimer.Stop();
            sampleTimer.Elapsed -= UpdateMonitor;
        }
    }
    
    private void UpdateMonitor(object? sender, System.Timers.ElapsedEventArgs e)
    {
        hardwareMonitor.Update();
    }

    #endregion

    #region network

    private bool _isNetworkMonitorOpen;
    private void SetNetworkMonitor(bool enabled)
    {
        if (enabled != _isNetworkMonitorOpen)
        {
            var lastMonitorEnabled = IsMonitorOpen;

            _isNetworkMonitorOpen = enabled;
            hardwareMonitor.NetworkEnabled = enabled;

            SetMonitorTimer(lastMonitorEnabled, enabled);
        }
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

    public (string UploadSpeed, string DownloadSpeed) GetNetworkSpeed(bool showBps)
    {
        var (uploadSpeed, downloadSpeed) = hardwareMonitor.GetNetworkInfo();

        return (FormatSpeed(uploadSpeed, showBps), FormatSpeed(downloadSpeed, showBps));
    }

    public (string UploadSpeed, string DownloadSpeed) GetInitNetworkSpeed(bool showBps)
    {
        return (FormatSpeed(0, showBps), FormatSpeed(0, showBps));
    }

    #endregion

    #region cpu & gpu * memory

    private bool _isCpuGpuMemoryMonitorOpen;
    private void SetCpuGpuMemoryMonitor(bool enabled)
    {
        if (enabled != _isCpuGpuMemoryMonitorOpen)
        {
            var lastMonitorEnabled = IsMonitorOpen;

            _isCpuGpuMemoryMonitorOpen = enabled;
            hardwareMonitor.CpuEnabled = enabled;
            hardwareMonitor.GpuEnabled = enabled;
            hardwareMonitor.MemoryEnabled = enabled;

            SetMonitorTimer(lastMonitorEnabled, enabled);
        }
    }

    private static string FormatPercentage(float? percentage)
    {
        if (percentage == null)
        {
            return string.Empty;
        }

        return $"{percentage:F2} %";
    }

    private static string FormatTemperature(float? temperature, bool useCelsius)
    {
        if (temperature is null)
        {
            return string.Empty;
        }

        if (useCelsius)
        {
            return $"{temperature:F2} °C";
        }
        else
        {
            temperature = temperature * 9 / 5 + 32;
            return $"{temperature:F2} °F";
        }
    }

    private static string FormatMemoryUsedInfo(float? used, float? available)
    {
        if (used is null || available is null)
        {
            return string.Empty;
        }

        const ulong kilo = 1024;

        var total = used + available;
        if (total < 1 && total > 1 / 1024)
        {
            return $"{used * kilo:F2} / {total * kilo:F2} MB";

            // Don't support if memory is smaller than 1 MB. It's too small nowadays. :(
        }
        else if (total > 1024)
        {
            return $"{used / kilo:F2} / {total / kilo:F2} TB";

            // Don't support if memory is larger than 1 TB. It's too large nowadays. :(
        }

        return $"{used:F2} / {total:F2} GB";
    }

    public (string CpuLoad, string CpuTempreture) GetCpuInfo(bool useCelsius)
    {
        var (cpuLoad, cpuTemperature) = hardwareMonitor.GetCpuInfo();

        return (FormatPercentage(cpuLoad), FormatTemperature(cpuTemperature, useCelsius));
    }

    public (string CpuLoad, string CpuTempreture) GetInitCpuInfo(bool useCelsius)
    {
        return (FormatPercentage(0), FormatTemperature(0, useCelsius));
    }

    public (string GpuLoad, string GpuTempreture) GetGpuInfo(bool useCelsius)
    {
        var (_, gpuLoad, gpuTemperature) = hardwareMonitor.GetGpuInfo();

        return (FormatPercentage(gpuLoad), FormatTemperature(gpuTemperature, useCelsius));
    }

    public (string GpuLoad, string GpuTempreture) GetInitGpuInfo(bool useCelsius)
    {
        return (FormatPercentage(0), FormatTemperature(0, useCelsius));
    }

    public (string MemoryLoad, string MemoryUsedInfo) GetMemoryInfo()
    {
        var (memoryLoad, memoryUsed, memoryAvailable) = hardwareMonitor.GetMemoryInfo();

        return (FormatPercentage(memoryLoad), FormatMemoryUsedInfo(memoryUsed, memoryAvailable));
    }

    public (string MemoryLoad, string MemoryUsedInfo) GetInitMemoryInfo()
    {
        return (FormatPercentage(0), FormatMemoryUsedInfo(0, 0));
    }

    #endregion
}
