using CommunityToolkit.Mvvm.ComponentModel;

using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Contracts.ViewModels;
using DesktopWidgets3.Views.SubPages;

namespace DesktopWidgets3.ViewModels.Pages;

public partial class TimingViewModel : ObservableRecipient, INavigationAware
{
    public ISubNavigationService SubNavigationService
    {
        get;
    }

    private readonly ITimersService _timersService;

#if DEBUG
    private int i = 0;
#endif

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

#if DEBUG
        // for test only: why cannot show two windows in one time?
        if (i == 0)
        {
            App.ShowClockWindow();
            i++;
        }
        else
        {
            App.ShowCPUWindow();
        }
#endif
    }

    public void OnNavigatedFrom()
    {
        _timersService.StopUpdateTimeTimer();
    }
}
