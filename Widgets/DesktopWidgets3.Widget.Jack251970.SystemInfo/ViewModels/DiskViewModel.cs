using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Timer = System.Timers.Timer;

namespace DesktopWidgets3.Widget.Jack251970.SystemInfo.ViewModels;

public partial class DiskViewModel : ObservableRecipient
{
    private static string ClassName => typeof(DiskViewModel).Name;

    #region view properties

    public ObservableCollection<ProgressCardData> ProgressCardItems { get; set; } =
    [
        new ProgressCardData()
        {
            LeftTitle = "C:",
            RightTitle = "--",
            ProgressValue = 0
        }
    ];

    #endregion

    #region settings



    #endregion

    public string Id;

    private readonly DispatcherQueue _dispatcherQueue;

    private readonly HardwareInfoService _hardwareInfoService;

    private readonly Timer updateTimer = new();

    private bool listUpdating = false;

    public DiskViewModel(string widgetId, HardwareInfoService hardwareInfoService)
    {
        Id = widgetId;
        _dispatcherQueue = Main.WidgetInitContext.WidgetService.GetDispatcherQueue(Id);
        _hardwareInfoService = hardwareInfoService;
        InitializeAllTimers();
    }

    #region Timer Methods

    private void InitializeAllTimers()
    {
        InitializeTimer(updateTimer, UpdateDisk);
    }

    private static void InitializeTimer(Timer timer, Action action)
    {
        timer.AutoReset = true;
        timer.Enabled = false;
        timer.Interval = 1000;
        timer.Elapsed += (s, e) => action();
    }

    public void StartAllTimers()
    {
        updateTimer.Start();
    }

    public void StopAllTimers()
    {
        updateTimer.Stop();
    }

    public void DisposeAllTimers()
    {
        updateTimer.Dispose();
    }

    #endregion

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

            _dispatcherQueue.TryEnqueue(() =>
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

    #region Settings Methods

    public void LoadSettings(BaseWidgetSettings settings)
    {
        if (settings is DiskSettings)
        {

        }
    }

    #endregion
}
