using System.Collections.ObjectModel;
using Microsoft.UI.Dispatching;
using Timer = System.Timers.Timer;

namespace DesktopWidgets3.Widget.Jack251970.SystemInfo.ViewModels;

public partial class DiskViewModel : BaseWidgetViewModel, IWidgetUpdate, IWidgetWindowClose
{
    private static string ClassName => typeof(DiskViewModel).Name;

    #region view properties

    public ObservableCollection<ProgressCardData> ProgressCardItems { get; set; } = [];

    #endregion

    #region settings



    #endregion

    private readonly HardwareInfoService _hardwareInfoService;

    private readonly Timer updateTimer = new();

    private bool listUpdating = false;

    public DiskViewModel(HardwareInfoService hardwareInfoService)
    {
        _hardwareInfoService = hardwareInfoService;

        InitializeTimer(updateTimer, UpdateDisk);
    }

    private static void InitializeTimer(Timer timer, Action action)
    {
        timer.AutoReset = true;
        timer.Enabled = false;
        timer.Interval = 1000;
        timer.Elapsed += (s, e) => action();
    }

    #region Update Methods

    private void UpdateDisk()
    {
        try
        {
            var diskStats = _hardwareInfoService.GetDiskStats();

            if (diskStats == null)
            {
                return;
            }

            var progressCardData = new List<ProgressCardData>();

            for (var i = 0; i < diskStats.GetDiskCount(); i++)
            {
                var diskUsage = diskStats.GetDiskUsage(i);
                var diskPartitions = diskUsage.PartitionDatas;
                foreach (var partition in diskPartitions)
                {
                    if (partition.Name != null)
                    {
                        var loadValue = partition.Size == 0 ? 0f : (partition.Size - partition.FreeSpace) * 100f / partition.Size;

                        progressCardData.Add(new ProgressCardData()
                        {
                            LeftTitle = partition.Name,
                            RightTitle = FormatUtils.FormatUsedInfoByte(partition.Size - partition.FreeSpace, partition.Size),
                            ProgressValue = loadValue
                        });
                    }
                }
            }

            progressCardData.Sort((x, y) => string.Compare(x.LeftTitle, y.LeftTitle, StringComparison.Ordinal));

            if (progressCardData.Count == 0)
            {
                return;
            }

            DispatcherQueue.TryEnqueue(() =>
            {
                if (listUpdating)
                {
                    return;
                }

                listUpdating = true;

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
                    Main.WidgetInitContext.LogService.LogError(ClassName, e, "Disk Widget Update Error on DispatcherQueue");
                }

                listUpdating = false;
            });
        }
        catch (Exception e)
        {
            Main.WidgetInitContext.LogService.LogError(ClassName, e, "Disk Widget Update Error");
        }
    }

    #endregion

    #region Abstract Methods

    protected override void LoadSettings(BaseWidgetSettings settings, bool initialized)
    {
        // initialize or update widget from settings
        if (settings is DiskSettings)
        {

        }

        // initialize widget
        if (initialized)
        {
            ProgressCardItems.Add(new ProgressCardData()
            {
                LeftTitle = "C:",
                RightTitle = "--",
                ProgressValue = 0
            });

            updateTimer.Start();
        }
    }

    #endregion

    #region IWidgetUpdate

    public void EnableUpdate(bool enable)
    {
        updateTimer.Enabled = enable;
    }

    #endregion

    #region IWidgetWindowClose

    public void WidgetWindowClosing()
    {
        updateTimer.Dispose();
    }

    #endregion
}
