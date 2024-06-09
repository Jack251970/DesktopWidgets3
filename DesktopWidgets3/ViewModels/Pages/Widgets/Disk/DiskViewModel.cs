using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DesktopWidgets3.ViewModels.Pages.Widgets;

public partial class DiskViewModel : BaseWidgetViewModel<DiskWidgetSettings>, IWidgetUpdate, IWidgetClose
{
    #region view properties

    public ObservableCollection<ProgressCardData> ProgressCardItems { get; set; } = new();

    #endregion

    #region settings

    #endregion

    private readonly ISystemInfoService _systemInfoService;
    private readonly ITimersService _timersService;

    public DiskViewModel(ISystemInfoService systemInfoService, ITimersService timersService)
    {
        _systemInfoService = systemInfoService;
        _timersService = timersService;

        timersService.AddTimerAction(WidgetType.Disk, UpdateDisk);
    }

    private void UpdateDisk()
    {
        UpdateCards(false);
    }

    private void UpdateCards(bool init)
    {
        if (init)
        {
            var diskInfo = _systemInfoService.GetInitDiskInfo();
            var progressCardData = diskInfo.GetProgressCardData();

            ProgressCardItems.Clear();
            foreach (var item in progressCardData)
            {
                ProgressCardItems.Add(item);
            }
        }
        else
        {
            var diskInfo = _systemInfoService.GetDiskInfo();
            var progressCardData = diskInfo.GetProgressCardData();
            var dataCount = progressCardData.Count;
            var itemsCount = ProgressCardItems.Count;

            // Remove extra items
            if (dataCount < itemsCount)
            {
                var start = dataCount;
                var end = itemsCount;
                RunOnDispatcherQueue(() =>
                {       
                    for (var i = start; i < end; i++)
                    {      
                        // TODO: Fix Index out of range exception.
                        ProgressCardItems.RemoveAt(i);
                    }
                });

                itemsCount = dataCount;
            }

            // Update items
            for (var i = 0; i < itemsCount; i++)
            {
                var index = i;
                if (ProgressCardItems[i].LeftTitle != progressCardData[i].LeftTitle)
                {
                    var data = progressCardData[index].LeftTitle;
                    RunOnDispatcherQueue(() =>
                    {
                        ProgressCardItems[index].LeftTitle = data;
                    });
                }
                if (ProgressCardItems[i].RightTitle != progressCardData[i].RightTitle)
                {
                    var data = progressCardData[index].RightTitle;
                    RunOnDispatcherQueue(() =>
                    {
                        ProgressCardItems[index].RightTitle = data;
                    });
                }
                if (ProgressCardItems[i].ProgressValue != progressCardData[i].ProgressValue)
                {
                    var data = progressCardData[index].ProgressValue;
                    RunOnDispatcherQueue(() =>
                    {
                        ProgressCardItems[index].ProgressValue = data;
                    });
                }
            }

            // Add extra items
            if (dataCount > itemsCount)
            {
                var data = progressCardData.Skip(itemsCount).ToList();
                RunOnDispatcherQueue(() =>
                {
                    foreach (var item in data)
                    {
                        ProgressCardItems.Add(item);
                    }
                });
            }
        }
    }

    #region abstract methods

    protected override void LoadSettings(DiskWidgetSettings settings)
    {
        UpdateCards(true);
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
            _timersService.StartTimer(WidgetType.Disk);
        }
        else
        {
            _timersService.StopTimer(WidgetType.Disk);
        }
        await Task.CompletedTask;
    }

    public void WidgetWindow_Closing()
    {
        _timersService.RemoveTimerAction(WidgetType.Disk, UpdateDisk);
    }

    #endregion
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
