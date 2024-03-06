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

    public NetworkSpeedInfo GetNetworkSpeed(bool useBps)
    {
        NetworkSpeedInfo.ClearItems();

        var networkInfoItems = hardwareMonitor.GetNetworkInfo();
        foreach (var networkInfoItem in networkInfoItems)
        {
            var hardwareName = networkInfoItem.Identifier == HardwareMonitor.TotalSpeedHardwareIdentifier
                ? "Total".GetLocalized()
                : networkInfoItem.Name;

            NetworkSpeedInfo.AddItem(hardwareName, networkInfoItem.Identifier, FormatNetworkSpeed(networkInfoItem.UploadSpeed, useBps), FormatNetworkSpeed(networkInfoItem.DownloadSpeed, useBps));
        }

        return NetworkSpeedInfo;
    }

    public NetworkSpeedInfo GetInitNetworkSpeed(bool useBps)
    {
        NetworkSpeedInfo.ClearItems();

        NetworkSpeedInfo.AddItem("Total".GetLocalized(), HardwareMonitor.TotalSpeedHardwareIdentifier, FormatNetworkSpeed(0, useBps), FormatNetworkSpeed(0, useBps));

        return NetworkSpeedInfo;
    }

    private static string FormatNetworkSpeed(float? bytes, bool useBps)
    {
        if (bytes is null)
        {
            return string.Empty;
        }

        var unit = useBps ? "bps" : "B/s";
        if (useBps)
        {
            bytes *= 8;
        }

        return FormatBytes(bytes.Value, unit);
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

    private static string FormatMemoryUsedInfo(float? used, float? available)
    {
        if (used is null || available is null)
        {
            return string.Empty;
        }

        return FormateUsedInfoGB(used, used + available);
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
        foreach (var diskInfoItem in diskInfoItems)
        {
            foreach (var partitionInfoItem in diskInfoItem.PartitionInfoItems)
            {
                if (partitionInfoItem.Name != null)
                {
                    var loadValue = partitionInfoItem.PartitionUsed / partitionInfoItem.PartitionTotal * 100f;
                    DiskInfo.AddItem(partitionInfoItem.Name, partitionInfoItem.Identifier, FormatPercentage(loadValue), loadValue ?? 0, FormatDiskUsedInfo(partitionInfoItem.PartitionUsed, partitionInfoItem.PartitionTotal));
                }
            }
        }
        return DiskInfo;
    }

    public DiskInfo GetInitDiskInfo()
    {
        DiskInfo.ClearItems();

        DiskInfo.AddItem("C:", null!, FormatPercentage(0), 0, FormatDiskUsedInfo(0, 0));

        return DiskInfo;
    }

    private static string FormatDiskUsedInfo(float? used, float? total)
    {
        if (used is null || total is null)
        {
            return string.Empty;
        }

        return FormateUsedInfoB(used, total);
    }

    #endregion

    #region format methods

    private const ulong kilo = 1024;
    private const ulong mega = 1024 * kilo;
    private const ulong giga = 1024 * mega;
    private const ulong kiloGiga = 1024 * giga;
    private const float recKilo = 1f / kilo;
    private const float recMega = 1f / mega;
    private const float recGiga = 1f / giga;
    private const float recKiloGiga = 1f / kiloGiga;

    private static readonly string PercentageFormat = "{0:F2} %";
    private static readonly string BytesFormat = "{0:F2} {1}";
    private static readonly string CelsiusTemperatureFormat = "{0:F2} °C";
    private static readonly string FahrenheitTemperatureFormat = "{0:F2} °C";
    private static readonly string UsedInfoFormat = "{0:F2} / {1:F2} {2}";

    private static string FormatPercentage(float? percentage)
    {
        if (percentage == null)
        {
            return string.Empty;
        }

        return string.Format(PercentageFormat, percentage);
    }

    private static string FormatBytes(float? bytes, string unit)
    {
        if (bytes is null)
        {
            return string.Empty;
        }

        if (bytes < kilo)
        {
            return string.Format(BytesFormat, bytes, unit);
        }
        else if (bytes < mega)
        {
            return string.Format(BytesFormat, bytes / kilo, $"K{unit}");
        }
        else if (bytes < giga)
        {
            return string.Format(BytesFormat, bytes / mega, $"M{unit}");
        }
        else
        {
            return string.Format(BytesFormat, bytes / giga, $"G{unit}");
        }
    }

    private static string FormatTemperature(float? celsiusDegree, bool useCelsius)
    {
        if (celsiusDegree is null)
        {
            return string.Empty;
        }

        if (useCelsius)
        {
            return string.Format(CelsiusTemperatureFormat, celsiusDegree);
        }
        else
        {
            var fahrenheitDegree = celsiusDegree * 9 / 5 + 32;
            return string.Format(FahrenheitTemperatureFormat, fahrenheitDegree);
        }
    }

    private static string FormateUsedInfoGB(float? used, float? total)
    {
        if (used is null || total is null)
        {
            return string.Empty;
        }

        if (total > 1024)
        {
            return string.Format(UsedInfoFormat, used * recKilo, total * recKilo, "TB");
        }
        else if (total > 1)
        {
            return string.Format(UsedInfoFormat, used, total, "GB");
        }
        else if (total > recKilo)
        {
            return string.Format(UsedInfoFormat, used * kilo, total * kilo, "MB");
        }
        else if (total > recMega)
        {
            return string.Format(UsedInfoFormat, used * mega, total * mega, "KB");
        }
        else if (total > recGiga)
        {
            return string.Format(UsedInfoFormat, used * giga, total * giga, "B");
        }
        else
        {
            return string.Format(UsedInfoFormat, used * giga * 8, total * giga * 8, "b");
        }
    }

    private static string FormateUsedInfoB(float? used, float? total)
    {
        if (used is null || total is null)
        {
            return string.Empty;
        }

        if (total > kiloGiga)
        {
            return string.Format(UsedInfoFormat, used * recKiloGiga, total * recKiloGiga, "TB");
        }
        else if (total > giga)
        {
            return string.Format(UsedInfoFormat, used * recGiga, total * recGiga, "GB");
        }
        else if (total > mega)
        {
            return string.Format(UsedInfoFormat, used * recMega, total * recMega, "MB");
        }
        else if (total > kilo)
        {
            return string.Format(UsedInfoFormat, used * recKilo, total * recKilo, "KB");
        }
        else if (total > 1)
        {
            return string.Format(UsedInfoFormat, used, total, "B");
        }
        else
        {
            return string.Format(UsedInfoFormat, used * 8, total * 8, "b");
        }
    }

    #endregion
}
