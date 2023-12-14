using DesktopWidgets3.Contracts.Services;
using Hardware.Info;

namespace DesktopWidgets3.Services;

public class SystemInfoService : ISystemInfoService
{
    private readonly IHardwareInfo hardwareInfo = new HardwareInfo();

    public SystemInfoService()
    {
        
    }

    public (string UploadSpeed, string DownloadSpeed) GetNetworkSpeed(bool showBps)
    {
        hardwareInfo.RefreshNetworkAdapterList();

        ulong totalBytesSentPersec = 0;
        ulong totalBytesReceivedPersec = 0;

        foreach (var hardware in hardwareInfo.NetworkAdapterList)
        {
            totalBytesSentPersec += hardware.BytesSentPersec;
            totalBytesReceivedPersec += hardware.BytesReceivedPersec;
        }

        return (FormatSpeed(totalBytesReceivedPersec, showBps), FormatSpeed(totalBytesSentPersec, showBps));  
    }

    private static string FormatSpeed(ulong bytes, bool showBps)
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
            return $"{bytes * 1.0 / kilobyte:F2} K{unit}";
        }
        else if (bytes < gigabyte)
        {
            return $"{bytes * 1.0 / megabyte:F2} M{unit}";
        }
        else
        {
            return $"{bytes * 1.0 / gigabyte:F2} G{unit}";
        }
    }
}
