using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Contracts.ViewModels;

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

    public StatisticViewModel()
    {
    }

    public void OnNavigatedTo(object parameter)
    {
        
    }

    public void OnNavigatedFrom()
    {
        
    }
}
