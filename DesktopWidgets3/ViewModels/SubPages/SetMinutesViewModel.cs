using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Contracts.ViewModels;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.Services;
using DesktopWidgets3.Views.SubPages;

namespace DesktopWidgets3.ViewModels.SubPages;

public partial class SetMinutesViewModel : ObservableRecipient, INavigationAware
{
    [ObservableProperty]
    private string _inputTip = string.Empty;
    [ObservableProperty]
    private int _timingMinutes = 60;
    [ObservableProperty]
    private int _timingMinutesMinimum = 2;
    [ObservableProperty]
    private int _timingMinutesMaximum = 720;
    [ObservableProperty]
    private string _startTimingContent = string.Empty;

    private readonly ISubNavigationService _subNavigationService;

    private bool nowLocking;

    public SetMinutesViewModel(ISubNavigationService subNavigationService)
    {
        _subNavigationService = subNavigationService;
    }

    public void OnNavigatedTo(object parameter)
    {
        if (parameter is Dictionary<string, object> parameters)
        {
            if (parameters.ContainsKey("InputTip"))
            {
                InputTip = (string)parameters["InputTip"];
            }
            if (parameters.ContainsKey("TimingMinutesMinimum"))
            {
                TimingMinutesMinimum = (int)parameters["TimingMinutesMinimum"];
            }
            if (parameters.ContainsKey("TimingMinutesMaximum"))
            {
                TimingMinutesMaximum = (int)parameters["TimingMinutesMaximum"];
            }
            // You need to change its maximum value before changing its value because its value maybe larger than maximum value
            if (parameters.ContainsKey("DefaultTimingMinutes"))
            {
                TimingMinutes = (int)parameters["DefaultTimingMinutes"];
            }
            if (parameters.ContainsKey("NowLocking"))
            {
                nowLocking = (bool)parameters["NowLocking"];
                StartTimingContent = (nowLocking ? "SetMinutes_StartTiming_Locking" : "SetMinutes_StartTiming_Relaxing").GetLocalized();
            }
        }
    }

    public void OnNavigatedFrom()
    {

    }

    [RelayCommand]
    private void OnStartTiming()
    {
        Dictionary<string, object> parameter = new()
        {
            {
                "TimingMinutes",
                TimingMinutes
            },
            {
                "NowLocking",
                nowLocking
            },
        };
        _subNavigationService.NavigateTo(typeof(MainTimingPage), parameter);
    }
}
