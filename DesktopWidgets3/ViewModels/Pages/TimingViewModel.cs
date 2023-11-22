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

#if DEBUG
    private int i = 0;
#endif

    public TimingViewModel(ISubNavigationService subNavigationService, ITimersService timersService, IWidgetManagerService widgetManagerService)
    {
        SubNavigationService = subNavigationService;
        _timersService = timersService;

        widgetManagerService.ShowWidget("Clock");
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
        // for test only: run in debug mode
        /*if (i == 0)
        {
            App.ShowClockWindow();
            i++;
        }
        else
        {
            App.ShowCPUWindow();
        }*/
        // for test only: run the exe file directly
        /*App.ShowClockWindow();
        App.ShowCPUWindow();*/
#endif
    }

    public void OnNavigatedFrom()
    {
        _timersService.StopUpdateTimeTimer();
    }
}
