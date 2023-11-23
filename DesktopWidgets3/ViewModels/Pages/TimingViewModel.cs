using CommunityToolkit.Mvvm.ComponentModel;

using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Contracts.ViewModels;
using DesktopWidgets3.Services;
using DesktopWidgets3.Views.SubPages;

namespace DesktopWidgets3.ViewModels.Pages;

public partial class TimingViewModel : ObservableRecipient, INavigationAware
{
    public ISubNavigationService SubNavigationService
    {
        get;
    }

    private readonly ITimersService _timersService;

    public TimingViewModel(ISubNavigationService subNavigationService, ITimersService timersService)
    {
        SubNavigationService = subNavigationService;
        _timersService = timersService;
    }

    public void OnNavigatedTo(object parameter)
    {
        if (parameter is Dictionary<string, object>)
        {
            SubNavigationService.NavigateTo(typeof(MainTimingPage), parameter);
        }
        else
        {
            _timersService.StartUpdateTimeTimer();
        }
    }

    public void OnNavigatedFrom()
    {
        _timersService.StopUpdateTimeTimer();
    }
}
