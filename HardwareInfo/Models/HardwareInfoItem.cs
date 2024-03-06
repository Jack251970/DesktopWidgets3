namespace HardwareInfo.Models;

public class HardwareInfoItem
{
    public string Name { get; set; } = null!;

    public string Identifier { get; set; } = null!;
}

public class NetworkInfoItem : HardwareInfoItem
{
    public float? UploadSpeed { get; set; }

    public float? DownloadSpeed { get; set; }
}

public class DiskInfoItem : HardwareInfoItem
{
    public List<PartitionInfoItem> PartitionInfoItems { get; set; } = null!;

    public float? DiskUsed { get; set; }

    public float? DiskAvailable { get; set; }
}

public class PartitionInfoItem : HardwareInfoItem
{
    public float? PartitionUsed { get; set; }

    public float? PartitionAvailable { get; set; }
}
