using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DesktopWidgets3.ViewModels.Pages.Widgets;

public partial class DiskViewModel : BaseWidgetViewModel<DiskWidgetSettings>, IWidgetUpdate, IWidgetClose
{
    private static string ClassName => typeof(DiskViewModel).Name;

    #region view properties

    public ObservableCollection<ProgressCardData> ProgressCardItems { get; set; } = new();

    #endregion

    #region settings

    #endregion

    private readonly ISystemInfoService _systemInfoService;

    private bool updating = false;

    public DiskViewModel(ISystemInfoService systemInfoService)
    {
        _systemInfoService = systemInfoService;

        _systemInfoService.RegisterUpdatedCallback(HardwareType.Disk, UpdateDisk);
    }

    private void UpdateDisk()
    {
        try
        {
            var diskStats = _systemInfoService.GetDiskStats();

            if (diskStats == null)
            {
                return;
            }

            var progressCardData = GetDiskInfo(diskStats).GetProgressCardData();

            if (progressCardData.Count == 0)
            {
                return;
            }

            RunOnDispatcherQueue(() =>
            {
                if (updating)
                {
                    return;
                }

                updating = true;

                try
                {
                    var dataCount = progressCardData.Count;
                    var itemsCount = ProgressCardItems.Count;

                    // Remove extra items
                    if (dataCount < itemsCount)
                    {
                        for (var i = dataCount; i < itemsCount; i++)
                        {
                            ProgressCardItems.RemoveAt(i);
                        }

                        itemsCount = dataCount;
                    }

                    // Update items
                    for (var i = 0; i < itemsCount; i++)
                    {
                        if (ProgressCardItems[i].LeftTitle != progressCardData[i].LeftTitle)
                        {
                            var data = progressCardData[i].LeftTitle;
                            ProgressCardItems[i].LeftTitle = data;
                        }
                        if (ProgressCardItems[i].RightTitle != progressCardData[i].RightTitle)
                        {
                            var data = progressCardData[i].RightTitle;
                            ProgressCardItems[i].RightTitle = data;
                        }
                        if (ProgressCardItems[i].ProgressValue != progressCardData[i].ProgressValue)
                        {
                            var data = progressCardData[i].ProgressValue;
                            ProgressCardItems[i].ProgressValue = data;
                        }
                    }

                    // Add extra items
                    if (dataCount > itemsCount)
                    {
                        var data = progressCardData.Skip(itemsCount).ToList();
                        foreach (var item in data)
                        {
                            ProgressCardItems.Add(item);
                        }
                    }
                }
                catch (Exception e)
                {
                    LogExtensions.LogError(ClassName, e, "Disk Widget Update Error on DispatcherQueue");
                }

                updating = false;
            });
        }
        catch (Exception e)
        {
            LogExtensions.LogError(ClassName, e, "Disk Widget Update Error");
        }
    }

    private static DiskInfo GetDiskInfo(DiskStats diskStats)
    {
        var diskInfo = new DiskInfo();

        var diskCount = diskStats.GetDiskCount();
        for (var i = 0; i < diskCount; i++)
        {
            var diskUsage = diskStats.GetDiskUsage(i);
            var diskPartitions = diskUsage.PartitionDatas;
            foreach (var partition in diskPartitions)
            {
                if (partition.Name != null)
                {
                    var loadValue = partition.Size == 0 ? 0f : (partition.Size - partition.FreeSpace) * 100f / partition.Size;
                    diskInfo.AddItem(partition.Name, partition.DeviceId, FormatUtils.FormatPercentage(loadValue), loadValue, FormatUtils.FormatUsedInfoByte(partition.Size - partition.FreeSpace, partition.Size));
                }
            }
        }
        diskInfo.SortItems();

        return diskInfo;
    }

    #region abstract methods

    protected override void LoadSettings(DiskWidgetSettings settings)
    {
        ProgressCardItems.Add(new ProgressCardData()
        {
            LeftTitle = "C:",
            RightTitle = "--",
            ProgressValue = 0
        });
    }

    public override DiskWidgetSettings GetSettings()
    {
        return new DiskWidgetSettings()
        {

        };
    }

    #endregion

    #region interfaces

    public async Task EnableUpdate(bool enable)
    {
        if (enable)
        {
            _systemInfoService.RegisterUpdatedCallback(HardwareType.Disk, UpdateDisk);
        }
        else
        {
            _systemInfoService.UnregisterUpdatedCallback(HardwareType.Disk, UpdateDisk);
        }

        await Task.CompletedTask;
    }

    public void WidgetWindow_Closing()
    {
        _systemInfoService.UnregisterUpdatedCallback(HardwareType.Disk, UpdateDisk);
    }

    #endregion

    private class DiskInfo
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
}

public class ProgressCardData : INotifyPropertyChanged
{
    private string leftTitle = string.Empty;

    public string LeftTitle
    {
        get => leftTitle;
        set
        {
            if (value != leftTitle)
            {
                leftTitle = value;
                NotifyPropertyChanged(nameof(LeftTitle));
            }
        }
    }

    private string rightTitle = string.Empty;

    public string RightTitle
    {
        get => rightTitle;
        set
        {
            if (value != rightTitle)
            {
                rightTitle = value;
                NotifyPropertyChanged(nameof(RightTitle));
            }
        }
    }

    private double progressValue = 0;

    public double ProgressValue
    {
        get => progressValue;
        set
        {
            if (value != progressValue)
            {
                progressValue = value;
                NotifyPropertyChanged(nameof(ProgressValue));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
