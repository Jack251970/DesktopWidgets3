using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.UI.Dispatching;

namespace DesktopWidgets3.Widget.Jack251970.Disk.ViewModels;

public partial class DiskViewModel : BaseWidgetViewModel, IAsyncWidgetUpdate, IWidgetClose
{
    private static string ClassName => typeof(DiskViewModel).Name;

    #region view properties

    public ObservableCollection<ProgressCardData> ProgressCardItems { get; set; } = [];

    #endregion

    #region settings



    #endregion

    private readonly WidgetInitContext Context;

    private readonly HardwareInfoService _hardwareInfoService;

    private readonly Timer updateTimer = new();

    private bool listUpdating = false;

    public DiskViewModel(WidgetInitContext context, HardwareInfoService hardwareInfoService)
    {
        Context = context;

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

    #region update methods

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
                    Context.LogService.LogError(ClassName, e, "Disk Widget Update Error on DispatcherQueue");
                }

                listUpdating = false;
            });
        }
        catch (Exception e)
        {
            Context.LogService.LogError(ClassName, e, "Disk Widget Update Error");
        }
    }

    #endregion

    #region abstract methods

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

    #region widget update

    public async Task EnableUpdateAsync(bool enable)
    {
        updateTimer.Enabled = enable;

        await Task.CompletedTask;
    }

    #endregion

    #region widget closing

    public void OnWidgetClose()
    {
        updateTimer.Dispose();
    }

    #endregion
}
