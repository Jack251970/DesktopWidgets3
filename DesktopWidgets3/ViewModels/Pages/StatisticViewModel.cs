using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Contracts.ViewModels;
using DesktopWidgets3.Services;

namespace DesktopWidgets3.ViewModels.Pages;

public partial class StatisticViewModel : ObservableRecipient, INavigationAware
{
    [ObservableProperty]
    private string _completedTimes = string.Empty;

    [ObservableProperty]
    private string _totalTime = string.Empty;

    [ObservableProperty]
    private string _averageTime = string.Empty;

    [ObservableProperty]
    private string _todaySummary = string.Empty;

    [ObservableProperty]
    private string _todayCompletedTimes = string.Empty;

    [ObservableProperty]
    private string _todayTotalTime = string.Empty;

    [ObservableProperty]
    private string _dailyGoal = string.Empty;

    [ObservableProperty]
    private int _todayProgress = 40;

#if DEBUG
    private static int i = 0;
#endif

    private readonly IWidgetManagerService _widgetManagerService;

    public StatisticViewModel(IWidgetManagerService widgetManagerService)
    {
        _widgetManagerService = widgetManagerService;
    }

    public void OnNavigatedTo(object parameter)
    {
#if DEBUG
        // for test only: run in debug mode
        if (i == 0)
        {
            _widgetManagerService.ShowWidget("Clock");
            i++;
        }
        else if (i == 1)
        {
            _widgetManagerService.ShowWidget("CPU");
            i++;
        }
        else
        {
            i++;
        }
#endif
    }

    public void OnNavigatedFrom()
    {
        
    }
}
