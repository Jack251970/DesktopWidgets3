using System.Globalization;
using System.Timers;
using HardwareInfo.Helpers;

using Timer = System.Timers.Timer;

namespace DesktopWidgets3.Services.Widgets;

internal class SystemInfoService : ISystemInfoService
{
    private readonly IAppSettingsService _appSettingsService;

    private readonly HardwareMonitor hardwareMonitor = new();

    private readonly Timer sampleTimer = new();

    public SystemInfoService(IAppSettingsService appSettingsService)
    {
        _appSettingsService = appSettingsService;

        sampleTimer.AutoReset = true;
        sampleTimer.Enabled = false;
        sampleTimer.Interval = _appSettingsService.BatterySaver ? 1000 : 100;
        sampleTimer.Elapsed += SampleTimer_Elapsed;

        _appSettingsService.OnBatterySaverChanged += AppSettingsService_OnBatterySaverChanged;

        hardwareMonitor.EnabledChanged += HardwareMonitor_OnEnabledChanged;
    }

    private void SampleTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        hardwareMonitor.Update();
    }

    private void AppSettingsService_OnBatterySaverChanged(object? _, bool batterySaver)
    {
        var enabled = sampleTimer.Enabled;
        sampleTimer.Enabled = false;
        sampleTimer.Interval = batterySaver ? 1000 : 100;
        sampleTimer.Enabled = enabled;
    }

    private void HardwareMonitor_OnEnabledChanged(object? _, bool enabled)
    {
        if (hardwareMonitor.Enabled)
        {
            sampleTimer.Start();
        }
        else
        {
            sampleTimer.Stop();
        }
    }

    public void StartMonitor(WidgetType type)
    {
        switch (type)
        {
            case WidgetType.Network:
                hardwareMonitor.NetworkEnabled = true;
                break;
            case WidgetType.Performance:
                hardwareMonitor.CpuEnabled = true;
                hardwareMonitor.GpuEnabled = true;
                hardwareMonitor.MemoryEnabled = true;
                break;
            case WidgetType.Disk:
                hardwareMonitor.DiskEnabled = true;
                break;
        }
    }

    public void StopMonitor(WidgetType type)
    {
        switch (type)
        {
            case WidgetType.Network:
                hardwareMonitor.NetworkEnabled = false;
                break;
            case WidgetType.Performance:
                hardwareMonitor.CpuEnabled = false;
                hardwareMonitor.GpuEnabled = false;
                hardwareMonitor.MemoryEnabled = false;
                break;
            case WidgetType.Disk:
                hardwareMonitor.DiskEnabled = false;
                break;
        }
    }

    #region network

    private readonly NetworkSpeedInfo NetworkSpeedInfo = new();

    public NetworkSpeedInfo GetNetworkSpeed(bool useBps)
    {
        NetworkSpeedInfo.ClearItems();

        var currentData = hardwareMonitor.GetNetworkStats();

        if (currentData == null)
        {
            NetworkSpeedInfo.AddItem("Total".GetLocalized(), "Total", "--", "--");
            return NetworkSpeedInfo;
        }

        var netCount = currentData.GetNetworkCount();
        var totalSent = 0f;
        var totalReceived = 0f;
        for (var i = 0; i < netCount; i++)
        {
            var netName = currentData.GetNetworkName(i);
            var networkUsage = currentData.GetNetworkUsage(i);
            var uploadSpeed = FormatNetworkSpeed(networkUsage.Sent, useBps);
            var downloadSpeed = FormatNetworkSpeed(networkUsage.Received, useBps);
            NetworkSpeedInfo.AddItem(netName, netName, uploadSpeed, downloadSpeed);
            totalSent += networkUsage.Sent;
            totalReceived += networkUsage.Received;
        }

        var totalUploadSpeed = FormatNetworkSpeed(totalSent, useBps);
        var totalDownloadSpeed = FormatNetworkSpeed(totalReceived, useBps);
        NetworkSpeedInfo.InsertItem(0, "Total".GetLocalized(), "Total", totalUploadSpeed, totalDownloadSpeed);

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

    public (string CpuLoad, float CpuLoadValue, string CpuSpeed) GetCpuInfo()
    {
        var currentData = hardwareMonitor.GetCpuStats();

        if (currentData == null)
        {
            return (FormatPercentage(0), 0, "--");
        }

        var cpuUsage = FormatPercentage(currentData.CpuUsage);
        var cpuSpeed = FormatCpuSpeed(currentData.CpuSpeed);

        return (cpuUsage, currentData.CpuUsage, cpuSpeed);
    }

    public (string GpuName, string GpuLoad, float GpuLoadValue, string GpuInfo) GetGpuInfo(bool useCelsius)
    {
        var stats = hardwareMonitor.GetGpuStats();

        if (stats == null)
        {
            return (string.Empty, FormatPercentage(0), 0, "--");
        }

        // TODO: Add actite index support.
        var _gpuActiveIndex = 0;
        var gpuName = stats.GetGPUName(_gpuActiveIndex);
        var gpuUsage = stats.GetGPUUsage(_gpuActiveIndex);
        var gpuTemp = stats.GetGPUTemperature(_gpuActiveIndex);

        return (gpuName, FormatPercentage(gpuUsage), gpuUsage, gpuTemp == 0 ? string.Empty : FormatTemperature(gpuTemp, useCelsius));
    }

    public (string MemoryLoad, float MemoryLoadValue, string MemoryUsedInfo) GetMemoryInfo()
    {
        var currentData = hardwareMonitor.GetMemoryStats();

        if (currentData == null)
        {
            return (FormatPercentage(0), 0, "--");
        }

        var usedMem = currentData.UsedMem;
        var memUsage = currentData.MemUsage;
        var allMem = currentData.AllMem;

        return (FormatPercentage(memUsage), memUsage, FormatUsedInfoByte(usedMem, allMem));
    }

    #endregion

    #region disk

    private readonly DiskInfo DiskInfo = new();

    public DiskInfo GetDiskInfo()
    {
        DiskInfo.ClearItems();

        var diskInfoItems = hardwareMonitor.GetDiskInfo();

        if (diskInfoItems == null)
        {
            return DiskInfo;
        }

        var diskCount = diskInfoItems.GetDiskCount();
        for (var i = 0; i < diskCount; i++)
        {
            var diskUsage = diskInfoItems.GetDiskUsage(i);
            var diskPartitions = diskUsage.PartitionDatas;
            foreach (var partition in diskPartitions)
            {
                if (partition.Name != null)
                {
                    var loadValue = partition.Size == 0 ? 0f : (partition.Size - partition.FreeSpace) * 100f / partition.Size;
                    DiskInfo.AddItem(partition.Name, partition.DeviceId, FormatPercentage(loadValue), loadValue, FormatUsedInfoByte(partition.Size - partition.FreeSpace, partition.Size));
                }
            }
        }
        DiskInfo.SortItems();

        return DiskInfo;
    }

    #endregion

    #region format methods

    private const ulong Kilo = 1024;
    private const ulong Mega = 1024 * Kilo;
    private const ulong Giga = 1024 * Mega;
    private const ulong KiloGiga = 1024 * Giga;
    private const float RecKilo = 1f / Kilo;
    private const float RecMega = 1f / Mega;
    private const float RecGiga = 1f / Giga;
    private const float RecKiloGiga = 1f / KiloGiga;

    private static readonly string PercentageFormat = "{0:F2} %";
    private static readonly string CpuSpeedFormat = "{0:F2} GHz";
    private static readonly string BytesFormat = "{0:F2} {1}";
    private static readonly string CelsiusTemperatureFormat = "{0:F2} °C";
    private static readonly string FahrenheitTemperatureFormat = "{0:F2} °C";
    private static readonly string UsedInfoFormat = "{0:F2} / {1:F2} {2}";

    private static string FormatPercentage(float percentage)
    {
        return string.Format(CultureInfo.InvariantCulture, PercentageFormat, percentage * 100);
    }

    private static string FormatCpuSpeed(float cpuSpeed)
    {
        return string.Format(CultureInfo.InvariantCulture, CpuSpeedFormat, cpuSpeed / 1000);
    }

    private static string FormatBytes(float bytes, string unit)
    {
        if (bytes < Kilo)
        {
            return string.Format(CultureInfo.InvariantCulture, BytesFormat, bytes, unit);
        }
        else if (bytes < Mega)
        {
            return string.Format(CultureInfo.InvariantCulture, BytesFormat, bytes / Kilo, $"K{unit}");
        }
        else if (bytes < Giga)
        {
            return string.Format(CultureInfo.InvariantCulture, BytesFormat, bytes / Mega, $"M{unit}");
        }
        else
        {
            return string.Format(CultureInfo.InvariantCulture, BytesFormat, bytes / Giga, $"G{unit}");
        }
    }

    private static string FormatTemperature(float celsiusDegree, bool useCelsius)
    {
        if (useCelsius)
        {
            return string.Format(CultureInfo.InvariantCulture, CelsiusTemperatureFormat, celsiusDegree);
        }
        else
        {
            var fahrenheitDegree = celsiusDegree * 9 / 5 + 32;
            return string.Format(CultureInfo.InvariantCulture, FahrenheitTemperatureFormat, fahrenheitDegree);
        }
    }

    private static string FormatUsedInfoByte(ulong used, ulong total)
    {
        if (total < Kilo)
        {
            return string.Format(CultureInfo.InvariantCulture, UsedInfoFormat, used, total, "B");
        }
        else if (total < Mega)
        {
            return string.Format(CultureInfo.InvariantCulture, UsedInfoFormat, used * RecKilo, total * RecKilo, "KB");
        }
        else if (total < Giga)
        {
            return string.Format(CultureInfo.InvariantCulture, UsedInfoFormat, used * RecMega, total * RecMega, "MB");
        }
        else if (total < KiloGiga)
        {
            return string.Format(CultureInfo.InvariantCulture, UsedInfoFormat, used * RecGiga, total * RecGiga, "GB");
        }
        else
        {
            return string.Format(CultureInfo.InvariantCulture, UsedInfoFormat, used * RecKiloGiga, total * RecKiloGiga, "TB");
        }
    }

    #endregion
}
