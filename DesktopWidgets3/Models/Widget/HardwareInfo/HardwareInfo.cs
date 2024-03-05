using HardwareInfo.Models;

namespace DesktopWidgets3.Models.Widget.HardwareInfo;

public class NetworkSpeedInfo
{
    private readonly List<NetworkSpeedInfoItem> networkSpeedInfoItems = new();

    public void AddItem(string hardwareName, string hardwareIdentifier, string uploadSpeed, string downloadSpeed)
    {
        networkSpeedInfoItems.Add(new NetworkSpeedInfoItem()
        {
            HardwareName = hardwareName,
            HardwareIdentifier = hardwareIdentifier,
            UploadSpeed = uploadSpeed,
            DownloadSpeed = downloadSpeed
        });
    }

    public (int index, string HardwareName, string HardwareIdentifier, string UploadSpeed, string DownloadSpeed)? SearchItemByIdentifier(string hardwareIdentifier)
    {
        for (var i = 0; i < networkSpeedInfoItems.Count; i++)
        {
            if (networkSpeedInfoItems[i].HardwareIdentifier == hardwareIdentifier)
            {
                return (i, networkSpeedInfoItems[i].HardwareName, networkSpeedInfoItems[i].HardwareIdentifier, networkSpeedInfoItems[i].UploadSpeed, networkSpeedInfoItems[i].DownloadSpeed);
            }
        }

        return null;
    }

    public (int index, string HardwareName, string HardwareIdentifier, string UploadSpeed, string DownloadSpeed)? SearchItemByName(string hardwareName)
    {
        for (var i = 0; i < networkSpeedInfoItems.Count; i++)
        {
            if (networkSpeedInfoItems[i].HardwareName == hardwareName)
            {
                return (i, networkSpeedInfoItems[i].HardwareName, networkSpeedInfoItems[i].HardwareIdentifier, networkSpeedInfoItems[i].UploadSpeed, networkSpeedInfoItems[i].DownloadSpeed);
            }
        }

        return null;
    }

    public (int index, string HardwareName, string HardwareIdentifier, string UploadSpeed, string DownloadSpeed)? GetItem(int index)
    {
        if (index >= 0 && index < networkSpeedInfoItems.Count)
        {
            return (index, networkSpeedInfoItems[index].HardwareName, networkSpeedInfoItems[index].HardwareIdentifier, networkSpeedInfoItems[index].UploadSpeed, networkSpeedInfoItems[index].DownloadSpeed);
        }

        return null;
    }

    public List<Tuple<string, string>> GetHardwareNamesIdentifiers()
    {
        return networkSpeedInfoItems.Select(x => new Tuple<string, string>(x.HardwareName, x.HardwareIdentifier)).ToList();
    }

    public void ClearItems()
    {
        networkSpeedInfoItems.Clear();
    }

    private class NetworkSpeedInfoItem : HardwareInfoItem
    {
        public string UploadSpeed { get; set; } = null!;

        public string DownloadSpeed { get; set; } = null!;
    }
}
