namespace HardwareInfo.Models;

public class HardwareInfoItem
{
    public string HardwareName { get; set; } = null!;

    public string HardwareIdentifier { get; set; } = null!;
}

public class NetworkInfoItem : HardwareInfoItem
{
    public float? UploadSpeed { get; set; }

    public float? DownloadSpeed { get; set; }
}
