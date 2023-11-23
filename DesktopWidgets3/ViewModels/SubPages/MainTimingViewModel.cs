using System.Diagnostics;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Contracts.ViewModels;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.Views.SubPages;

namespace DesktopWidgets3.ViewModels.SubPages;

public partial class MainTimingViewModel : ObservableRecipient, INavigationAware
{
    [ObservableProperty]
    private bool _autoShutdown = false;
    [ObservableProperty]
    private string _killList = string.Empty;
    [ObservableProperty]
    private string _systemTime = string.Empty;
    [ObservableProperty]
    private string _systemDate = string.Empty;
    [ObservableProperty]
    private string _periodTip = string.Empty;
    [ObservableProperty]
    private string _timingPeriod = string.Empty;

    #region Const Strings

    private readonly string[] AddBlockNames =
    {
        "taskmgr",      // Task Manager
    };

    #endregion

    private DateTime _startTimingTime, _endTimingTime;

    private int haveLockingMinutes = 0;

    private string timingFormat = string.Empty;

    private readonly IAppNotificationService _appNotificationService;
    private readonly IAppSettingsService _appSettingsService;
    private readonly ISubNavigationService _subNavigationService;
    private readonly ITimersService _timersService;

    private readonly DispatcherQueue _dispatcherQueue = App.MainWindow!.DispatcherQueue;

    public MainTimingViewModel(IAppNotificationService appNotificationService, IAppSettingsService appSettingsService, ISubNavigationService subNavigationService, ITimersService timersService)
    {
        _appNotificationService = appNotificationService;
        _appSettingsService = appSettingsService;
        _subNavigationService = subNavigationService;
        _timersService = timersService;

        _timersService.InitializeUpdateTimeTimer(UpdateTime);
        _timersService.InitializeStopTimingTimer(StopTiming);
        _timersService.InitializeBreakReminderTimer(RemindBreak);
        _timersService.InitializeKillProcessesTimer(KillProcesses);
    }

    public void OnNavigatedTo(object parameter)
    {
        if (parameter is Dictionary<string, object> parameters)
        {
            if (parameters.ContainsKey("TimingMinutes") && parameters.ContainsKey("NowLocking"))
            {
                var timingMinutes = (int)parameters["TimingMinutes"];
                var nowLocking = (bool)parameters["NowLocking"];
                StartTiming(timingMinutes, nowLocking);
            }
            if (parameters.ContainsKey("StartLockTime") && parameters.ContainsKey("EndLockTime"))
            {
                var startLockTime = (DateTime)parameters["StartLockTime"];
                var endLockTime = (DateTime)parameters["EndLockTime"];
                StartTiming(startLockTime, endLockTime, true);
            }
        }
    }

    public void OnNavigatedFrom()
    {

    }

    public void StartTiming(int timingMinutes, bool nowLocking)
    {
        var startTimingTime = DateTime.Now;
#if DEBUG
        var endTimingTime = DateTime.Now.AddSeconds(timingMinutes);
#else
        var endTimingTime = DateTime.Now.AddMinutes(timingMinutes);
#endif
        StartTiming(startTimingTime, endTimingTime, nowLocking);
    }

    public async void StartTiming(DateTime startTimingTime, DateTime endTimingTime, bool nowLocking)
    {
        var showSeconds = await _appSettingsService.GetShowSecondsAsync();
        timingFormat = showSeconds ? "T" : "t";
        UpdateTime(DateTime.Now);
        KillList = string.Empty;
        var Start = startTimingTime.ToString("t");
        var End = endTimingTime.ToString("t");
        TimingPeriod = $@"{Start} - {End}";
        PeriodTip = nowLocking ? "MainTiming_PeriodTip_Locking".GetLocalized() : "MainTiming_PeriodTip_Relaxing".GetLocalized();
        _startTimingTime = startTimingTime;
        _endTimingTime = endTimingTime;
        _timersService.StartUpdateTimeStopTimingTimerAsync();
        if (nowLocking)
        {
            _timersService.StartBreakReminderKillProcessesTimerAsync();
            await _appSettingsService.SaveLockPeriod(_startTimingTime, _endTimingTime);
            _appSettingsService.IsLocking = true;
            _appNotificationService.Show(string.Format("MainTiming_AppNotification_StartLocking".GetLocalized(), AppContext.BaseDirectory));
        }
        else
        {
            _appSettingsService.IsRelaxing = true;
            haveLockingMinutes = 0;
            _appNotificationService.Show(string.Format("MainTiming_AppNotification_StartRelaxing".GetLocalized(), AppContext.BaseDirectory));
        }
    }

    private void UpdateTime(object? sender, EventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() => UpdateTime(DateTime.Now));
    }

    private void UpdateTime(DateTime now)
    {
        SystemTime = now.ToString(timingFormat);
        SystemDate = now.ToString("yyyy-MM-dd dddd");
    }

    private void StopTiming(object? sender, EventArgs e)
    {
        var Now = DateTime.Now;
        if (Now < _startTimingTime || Now > _endTimingTime)
        {
            _timersService.StopUpdateTimeStopTimingTimer();
            Dictionary<string, object> parameter = new();
            if (_appSettingsService.IsLocking)
            {
                var totalMinutes = (_endTimingTime - _startTimingTime).TotalMinutes;
                haveLockingMinutes += (int)totalMinutes;
                _timersService.StopBreakReminderKillProcessesTimerAsync();
                _appSettingsService.IsLocking = false;
                parameter.Add("CompleteTip", "CompleteTiming_CompleteTip_Locking".GetLocalized());
                parameter.Add("NextTimingButtonContent", "CompleteTiming_NextTiming_Locking".GetLocalized());
                parameter.Add("StartRelaxingButtonVisibility", (haveLockingMinutes >= 60) ? Visibility.Visible : Visibility.Collapsed);
                parameter.Add("haveLockingMinutes", haveLockingMinutes);
                _appNotificationService.Show(string.Format("MainTiming_AppNotification_StopLocking".GetLocalized(), AppContext.BaseDirectory));
            }
            else
            {
                _appSettingsService.IsRelaxing = false;
                parameter.Add("CompleteTip", "CompleteTiming_CompleteTip_Relaxing".GetLocalized());
                parameter.Add("NextTimingButtonContent", "CompleteTiming_NextTiming_Relaxing".GetLocalized());
                parameter.Add("StartRelaxingButtonVisibility", Visibility.Collapsed);
                parameter.Add("haveLockingMinutes", 0);
                _appNotificationService.Show(string.Format("MainTiming_AppNotification_StopRelaxing".GetLocalized(), AppContext.BaseDirectory));
            }
            _dispatcherQueue.TryEnqueue(() =>
            {
                _subNavigationService.NavigateTo(typeof(CompleteTimingPage), parameter);
                App.ShowMainWindow(true);
            });
            if (AutoShutdown)
            {
                SystemHelper.SystemPowerOff();
            }
        }
    }

    private void RemindBreak(object? sender, EventArgs e)
    {
        _appNotificationService.Show(string.Format("MainTiming_AppNotification_BreakReminder".GetLocalized(), AppContext.BaseDirectory));
    }

    private void KillProcesses(object? sender, EventArgs e)
    {
        var killedProcessIds = GetKilledProcessIds();

        var killedProcessNames = GetKilledProcessNames(killedProcessIds);

        ShowKilledProcessNames(killedProcessNames);
    }

    private List<int> GetKilledProcessIds()
    {
        var processesIds = new List<int>();

        try
        {
            var processes = Process.GetProcesses();
            foreach (var process in processes)
            {
                var processName = process.ProcessName.ToString();
                var blockList = _appSettingsService.GetBlockList();
                foreach (var exeName in blockList)
                {
#if DEBUG
                    if (exeName == "devenv.exe" || exeName == "setup.exe")
                    {
                        continue;
                    }
#endif
                    if (exeName[..^4].ToLower().Equals(processName.ToLower()))
                    {
                        processesIds.Add(process.Id);
                    }
                }
                foreach (var exeName in AddBlockNames)
                {
#if DEBUG
                    if (exeName == "taskmgr")
                    {
                        continue;
                    }
#endif
                    if (exeName.Equals(processName.ToLower()))
                    {
                        processesIds.Add(process.Id);
                    }
                }
            }
        }
        catch
        {

        }

        return processesIds;
    }

    private static List<string> GetKilledProcessNames(List<int> processIds)
    {
        var killedProcessNames = new List<string>() { };

        foreach (var processId in processIds)
        {
            try
            {
                var process = Process.GetProcessById(processId);
                var processName = process.ProcessName.ToString();
                process.Kill();
                if (!killedProcessNames.Contains(processName))
                {
                    killedProcessNames.Add(processName);
                }
            }
            catch
            {

            }
        }

        return killedProcessNames;
    }

    private void ShowKilledProcessNames(List<string> killedProcessNames)
    {
        if (killedProcessNames.Count > 0)
        {
            var killListText = new StringBuilder("MainTiming_KillProcessesTip".GetLocalized());
            foreach (var processName in killedProcessNames)
            {
                killListText.Append($"{processName}, ");
            }
            killListText.Remove(killListText.Length - 2, 2).Append('!');
            _dispatcherQueue.TryEnqueue(() => KillList = killListText.ToString());
            var task = Task.Run(async delegate
            {
                await Task.Delay(5000);
                _dispatcherQueue.TryEnqueue(() => KillList = string.Empty);
            });
            task.Wait();
        }
    }
}
