using HardwareInfo.Models;

namespace DesktopWidgets3.Models.Widget.HardwareInfo;

public class NetworkSpeedInfo
{
    private readonly List<NetworkSpeedInfoItem> networkSpeedInfoItems = new();

    public void AddItem(string hardwareName, string hardwareIdentifier, string uploadSpeed, string downloadSpeed)
    {
        networkSpeedInfoItems.Add(new NetworkSpeedInfoItem()
        {
            Name = hardwareName,
            Identifier = hardwareIdentifier,
            UploadSpeed = uploadSpeed,
            DownloadSpeed = downloadSpeed
        });
    }

    public (int index, string HardwareName, string HardwareIdentifier, string UploadSpeed, string DownloadSpeed)? SearchItemByIdentifier(string hardwareIdentifier)
    {
        for (var i = 0; i < networkSpeedInfoItems.Count; i++)
        {
            if (networkSpeedInfoItems[i].Identifier == hardwareIdentifier)
            {
                return (i, networkSpeedInfoItems[i].Name, networkSpeedInfoItems[i].Identifier, networkSpeedInfoItems[i].UploadSpeed, networkSpeedInfoItems[i].DownloadSpeed);
            }
        }

        return null;
    }

    public (int index, string HardwareName, string HardwareIdentifier, string UploadSpeed, string DownloadSpeed)? SearchItemByName(string hardwareName)
    {
        for (var i = 0; i < networkSpeedInfoItems.Count; i++)
        {
            if (networkSpeedInfoItems[i].Name == hardwareName)
            {
                return (i, networkSpeedInfoItems[i].Name, networkSpeedInfoItems[i].Identifier, networkSpeedInfoItems[i].UploadSpeed, networkSpeedInfoItems[i].DownloadSpeed);
            }
        }

        return null;
    }

    public (int index, string HardwareName, string HardwareIdentifier, string UploadSpeed, string DownloadSpeed)? GetItem(int index)
    {
        if (index >= 0 && index < networkSpeedInfoItems.Count)
        {
            return (index, networkSpeedInfoItems[index].Name, networkSpeedInfoItems[index].Identifier, networkSpeedInfoItems[index].UploadSpeed, networkSpeedInfoItems[index].DownloadSpeed);
        }

        return null;
    }

    public List<Tuple<string, string>> GetHardwareNamesIdentifiers()
    {
        return networkSpeedInfoItems.Select(x => new Tuple<string, string>(x.Name, x.Identifier)).ToList();
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

public class DiskInfo
{
    private readonly List<PartitionSpaceInfoItem> diskSpaceInfoItems = new();

    public void AddItem(string partitionName, string partitionIdentifier, string partitionLoad, float partitionLoadValue, string partitionUsedInfo)
    {
        diskSpaceInfoItems.Add(new PartitionSpaceInfoItem()
        {
            Name = partitionName,
            Identifier = partitionIdentifier,
            PartitionLoad = partitionLoad,
            PartitionLoadValue = partitionLoadValue,
            PartitionUsedInfo = partitionUsedInfo
        });
    }

    public List<ProgressCardData> GetProgressCardData()
    {
        return diskSpaceInfoItems.Select(x => new ProgressCardData()
        {
            LeftTitle = x.Name,
            RightTitle = x.PartitionUsedInfo,
            ProgressValue = x.PartitionLoadValue
        }).ToList();
    }

    public void ClearItems()
    {
        diskSpaceInfoItems.Clear();
    }

    private class PartitionSpaceInfoItem : PartitionInfoItem
    {
        public string PartitionLoad { get; set; } = null!;

        public float PartitionLoadValue { get; set; } = 0;

        public string PartitionUsedInfo { get; set; } = null!;
    }
}
