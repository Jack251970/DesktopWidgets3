using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Helpers;
using DesktopWidgets3.Views.SubPages;

namespace DesktopWidgets3.ViewModels.SubPages;

public partial class StartSettingViewModel : ObservableRecipient
{
    [ObservableProperty]
    private string _startLockingContent = "SetMinutes_StartTiming_Locking".GetLocalized();

    private readonly ISubNavigationService _subNavigationService;

    public StartSettingViewModel(ISubNavigationService subNavigationService)
    {
        _subNavigationService = subNavigationService;
    }

    [RelayCommand]
    private void OnStartSetting()
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
}
