using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Contracts.ViewModels;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.Views.SubPages;

namespace DesktopWidgets3.ViewModels.SubPages;

public partial class CompleteTimingViewModel : ObservableRecipient, INavigationAware
{
    [ObservableProperty]
    private string _completeTip = string.Empty;
    [ObservableProperty]
    private string _nextTimingButtonContent = string.Empty;
    [ObservableProperty]
    private string _startRelaxingButtonContent = "SetMinutes_StartTiming_Relaxing".GetLocalized();
    [ObservableProperty]
    private Visibility _startRelaxingButtonVisibility = Visibility.Collapsed;

    private readonly ISubNavigationService _subNavigationService;

    private int haveLockingMinutes;

    public CompleteTimingViewModel(ISubNavigationService subNavigationService)
    {
        _subNavigationService = subNavigationService;
    }

    public void OnNavigatedTo(object parameter)
    {
        if (parameter is Dictionary<string, object> parameters)
        {
            if (parameters.ContainsKey("CompleteTip"))
            {
                CompleteTip = (string)parameters["CompleteTip"];
            }
            if (parameters.ContainsKey("NextTimingButtonContent"))
            {
                NextTimingButtonContent = (string)parameters["NextTimingButtonContent"];
            }
            if (parameters.ContainsKey("StartRelaxingButtonVisibility"))
            {
                StartRelaxingButtonVisibility = (Visibility)parameters["StartRelaxingButtonVisibility"];
            }
            if (parameters.ContainsKey("haveLockingMinutes"))
            {
                haveLockingMinutes = (int)parameters["haveLockingMinutes"];
            }
        }
    }

    public void OnNavigatedFrom()
    {

    }

    [RelayCommand]
    private void OnStartLocking()
    {
        Dictionary<string, object> parameter = new()
        {
            {
                "InputTip",
                "SetMinutes_InputTip_Locking".GetLocalized()
            },
            {
                "TimingMinutesMinimum",
                2
            },
            {
                "TimingMinutesMaximum",
                720
            },
            {
                "DefaultTimingMinutes",
                60
            },
            {
                "NowLocking",
                true
            }
        };
        _subNavigationService.NavigateTo(typeof(SetMinutesPage), parameter);
    }

    [RelayCommand]
    private void OnStartRelaxing()
    {
        var timingMinutesMaximum = haveLockingMinutes / 4;
        Dictionary<string, object> parameter = new()
        {
            {
                "InputTip",
                string.Format("SetMinutes_InputTip_Relaxing".GetLocalized(), timingMinutesMaximum)
            },
            {
                "TimingMinutesMinimum",
                1
            },
            {
                "TimingMinutesMaximum",
                timingMinutesMaximum
            },
            {
                "DefaultTimingMinutes",
                haveLockingMinutes / 6
            },
            {
                "NowLocking",
                false
            }
        };
        _subNavigationService.NavigateTo(typeof(SetMinutesPage), parameter);
    }
}
