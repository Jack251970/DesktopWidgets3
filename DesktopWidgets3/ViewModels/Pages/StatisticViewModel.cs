using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Contracts.ViewModels;
using DesktopWidgets3.Helpers;

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

    private readonly IDataBaseService _dataBaseService;

    public StatisticViewModel(IDataBaseService dataBaseService)
    {
        _dataBaseService = dataBaseService;   
    }

    public void OnNavigatedTo(object parameter)
    {
        var times = _dataBaseService.GetTotalCompleteTimes();
        var minutes = _dataBaseService.GetTotalCompletedMinutes();
        CompletedTimes = times.ToString();
        TotalTime = GetTimeString(minutes);
        AverageTime = GetTimeString(times == 0 ? 0 : (minutes / times));

        TodaySummary = string.Format("Statistic_Today".GetLocalized(), DateTime.Now.ToString("yyyy-MM-dd"));
        _dataBaseService.GetTodayCompletedInfo(out var todayTimes, out var todayMinutes);
        TodayCompletedTimes = todayTimes.ToString();
        TodayTotalTime = GetTimeString(todayMinutes);

        TodayProgress = (int)(todayMinutes / 180.0 * 100.0);
        DailyGoal = GetTimeString(180);
    }

    public void OnNavigatedFrom()
    {
        
    }

    private string GetTimeString(int minutes)
    {
        if (minutes < 60)
        {
            return string.Format("Statistic_TimeTemplate_Minute".GetLocalized(), minutes);
        }
        else if (minutes >= 60 && minutes < 1440)
        {
            var hours = minutes / 60;
            var remainingMinutes = minutes % 60;
            return string.Format("Statistic_TimeTemplate_HourMinute".GetLocalized(), hours, remainingMinutes);
        }
        else
        {
            var days = minutes / 1440;
            var remainingMinutes = minutes % 1440;
            var hours = remainingMinutes / 60;
            var finalMinutes = remainingMinutes % 60;
            return string.Format("Statistic_TimeTemplate_DayHourMinute".GetLocalized(), days, hours, finalMinutes);
        }
    }
}
