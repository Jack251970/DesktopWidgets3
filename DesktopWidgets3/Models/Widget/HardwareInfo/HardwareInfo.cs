namespace DesktopWidgets3.Models.Widget.HardwareInfo;

public class NetworkSpeedInfo
{
    private readonly List<NetworkSpeedInfoItem> NetworkSpeedInfoItems = [];

    public void AddItem(string hardwareName, string hardwareIdentifier, string uploadSpeed, string downloadSpeed)
    {
        NetworkSpeedInfoItems.Add(new NetworkSpeedInfoItem()
        {
            Name = hardwareName,
            Identifier = hardwareIdentifier,
            UploadSpeed = uploadSpeed,
            DownloadSpeed = downloadSpeed
        });
    }

    public void InsertItem(int index, string hardwareName, string hardwareIdentifier, string uploadSpeed, string downloadSpeed)
    {
        NetworkSpeedInfoItems.Insert(index, new NetworkSpeedInfoItem()
        {
            Name = hardwareName,
            Identifier = hardwareIdentifier,
            UploadSpeed = uploadSpeed,
            DownloadSpeed = downloadSpeed
        });
    }

    public (int index, string HardwareName, string HardwareIdentifier, string UploadSpeed, string DownloadSpeed)? SearchItemByIdentifier(string hardwareIdentifier)
    {
        for (var i = 0; i < NetworkSpeedInfoItems.Count; i++)
        {
            if (NetworkSpeedInfoItems[i].Identifier == hardwareIdentifier)
            {
                return (i, NetworkSpeedInfoItems[i].Name, NetworkSpeedInfoItems[i].Identifier, NetworkSpeedInfoItems[i].UploadSpeed, NetworkSpeedInfoItems[i].DownloadSpeed);
            }
        }

        return null;
    }

    public List<Tuple<string, string>> GetHardwareNamesIdentifiers()
    {
        return NetworkSpeedInfoItems
            .Select(x => new Tuple<string, string>(x.Name, x.Identifier))
            .ToList();
    }

    private class NetworkSpeedInfoItem
    {
        public string Name { get; set; } = null!;

        public string Identifier { get; set; } = null!;

        public string UploadSpeed { get; set; } = null!;

        public string DownloadSpeed { get; set; } = null!;
    }
}

public class DiskInfo
{
    private readonly List<PartitionInfoItem> PartitionInfoItems = [];

    public void AddItem(string partitionName, string partitionIdentifier, string partitionLoad, float partitionLoadValue, string partitionUsedInfo)
    {
        PartitionInfoItems.Add(new PartitionInfoItem()
        {
            Name = partitionName,
            Identifier = partitionIdentifier,
            PartitionLoad = partitionLoad,
            PartitionLoadValue = partitionLoadValue,
            PartitionUsedInfo = partitionUsedInfo
        });
    }

    public void SortItems()
    {
        PartitionInfoItems.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
    }

    public List<ProgressCardData> GetProgressCardData()
    {
        return PartitionInfoItems.Select(x => new ProgressCardData()
        {
            LeftTitle = x.Name,
            RightTitle = x.PartitionUsedInfo,
            ProgressValue = x.PartitionLoadValue
        }).ToList();
    }

    private class PartitionInfoItem
    {
        public string Name { get; set; } = null!;

        public string Identifier { get; set; } = null!;

        public string PartitionLoad { get; set; } = null!;

        public float PartitionLoadValue { get; set; } = 0;

        public string PartitionUsedInfo { get; set; } = null!;
    }
}
