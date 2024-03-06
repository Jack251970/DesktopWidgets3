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
        sampleTimer.Interval = _appSettingsService.BatterySaver ? 1000 : 100;

        _appSettingsService.OnBatterySaverChanged += AppSettingsService_OnBatterySaverChanged;
    }

    private void AppSettingsService_OnBatterySaverChanged(object? _, bool batterySaver)
    {
        sampleTimer.Interval = batterySaver ? 1000 : 100;
    }

    #region monitor & sample timer

    private readonly HardwareMonitor hardwareMonitor = new();

    private readonly Timer sampleTimer = new();

    private bool isMonitorOpen = false;
    private void UpdateMonitorStatus()
    {
        var lastIsMonitorOpen = isMonitorOpen;

        isMonitorOpen = _isNetworkMonitorOpen || _isCpuGpuMemoryMonitorOpen || _isDiskMonitorOpen;

        // Check if need to change monitor status
        if (lastIsMonitorOpen == isMonitorOpen)
        {
            return;
        }

        if (isMonitorOpen)
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
            case WidgetType.Disk:
                SetDiskMonitor(true);
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
            case WidgetType.Disk:
                SetDiskMonitor(false);
                break;
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
            _isNetworkMonitorOpen = enabled;
            hardwareMonitor.NetworkEnabled = enabled;

            UpdateMonitorStatus();
        }
    }

    private readonly NetworkSpeedInfo NetworkSpeedInfo = new();

    private static string FormatSpeed(float? bytes, bool showBps)
    {
        if (bytes is null)
        {
            return string.Empty;
        }

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

    public NetworkSpeedInfo GetNetworkSpeed(bool showBps)
    {
        NetworkSpeedInfo.ClearItems();

        var networkInfoItems = hardwareMonitor.GetNetworkInfo();
        foreach (var networkInfoItem in networkInfoItems)
        {
            var hardwareName = networkInfoItem.Identifier == HardwareMonitor.TotalSpeedHardwareIdentifier
                ? "Total".GetLocalized()
                : networkInfoItem.Name;

            NetworkSpeedInfo.AddItem(hardwareName, networkInfoItem.Identifier, FormatSpeed(networkInfoItem.UploadSpeed, showBps), FormatSpeed(networkInfoItem.DownloadSpeed, showBps));
        }

        return NetworkSpeedInfo;
    }

    public NetworkSpeedInfo GetInitNetworkSpeed(bool showBps)
    {
        NetworkSpeedInfo.ClearItems();

        NetworkSpeedInfo.AddItem("Total".GetLocalized(), HardwareMonitor.TotalSpeedHardwareIdentifier, FormatSpeed(0, showBps), FormatSpeed(0, showBps));

        return NetworkSpeedInfo;
    }

    #endregion

    #region cpu & gpu * memory

    private bool _isCpuGpuMemoryMonitorOpen;
    private void SetCpuGpuMemoryMonitor(bool enabled)
    {
        if (enabled != _isCpuGpuMemoryMonitorOpen)
        {
            _isCpuGpuMemoryMonitorOpen = enabled;
            hardwareMonitor.CpuEnabled = enabled;
            hardwareMonitor.GpuEnabled = enabled;
            hardwareMonitor.MemoryEnabled = enabled;

            UpdateMonitorStatus();
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

    public (string CpuLoad, float CpuLoadValue, string CpuTempreture) GetCpuInfo(bool useCelsius)
    {
        var (cpuLoad, cpuTemperature) = hardwareMonitor.GetCpuInfo();

        return (FormatPercentage(cpuLoad), cpuLoad ?? 0, FormatTemperature(cpuTemperature, useCelsius));
    }

    public (string CpuLoad, float CpuLoadValue, string CpuTempreture) GetInitCpuInfo(bool useCelsius)
    {
        return (FormatPercentage(0), 0, FormatTemperature(0, useCelsius));
    }

    public (string GpuLoad, float GpuLoadValue, string GpuTempreture) GetGpuInfo(bool useCelsius)
    {
        var (_, gpuLoad, gpuTemperature) = hardwareMonitor.GetGpuInfo();

        return (FormatPercentage(gpuLoad), gpuLoad ?? 0, FormatTemperature(gpuTemperature, useCelsius));
    }

    public (string GpuLoad, float GpuLoadValue, string GpuTempreture) GetInitGpuInfo(bool useCelsius)
    {
        return (FormatPercentage(0), 0, FormatTemperature(0, useCelsius));
    }

    public (string MemoryLoad, float MemoryLoadValue, string MemoryUsedInfo) GetMemoryInfo()
    {
        var (memoryLoad, memoryUsed, memoryAvailable) = hardwareMonitor.GetMemoryInfo();

        return (FormatPercentage(memoryLoad), memoryLoad ?? 0, FormatMemoryUsedInfo(memoryUsed, memoryAvailable));
    }

    public (string MemoryLoad, float MemoryLoadValue, string MemoryUsedInfo) GetInitMemoryInfo()
    {
        return (FormatPercentage(0), 0, FormatMemoryUsedInfo(0, 0));
    }

    #endregion

    #region disk

    private bool _isDiskMonitorOpen;
    private void SetDiskMonitor(bool enabled)
    {
        if (enabled != _isDiskMonitorOpen)
        {
            _isDiskMonitorOpen = enabled;
            hardwareMonitor.DiskEnabled = enabled;

            UpdateMonitorStatus();
        }
    }

    private readonly DiskInfo DiskInfo = new();

    public DiskInfo GetDiskInfo()
    {
        DiskInfo.ClearItems();

        var diskInfoItems = hardwareMonitor.GetDiskInfo();

        return DiskInfo;
    }

    public DiskInfo GetInitDiskInfo()
    {
        DiskInfo.ClearItems();

        return DiskInfo;
    }

    #endregion
}
