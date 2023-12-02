using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Contracts.ViewModels;
using DesktopWidgets3.Models.Widget;
using Microsoft.UI.Dispatching;

namespace DesktopWidgets3.ViewModels.Pages.Widget.Clock;

public partial class ClockViewModel : ObservableRecipient, INavigationAware
{
    private readonly DispatcherQueue _dispatcherQueue = App.MainWindow!.DispatcherQueue;

    [ObservableProperty]
    private string _systemTime = string.Empty;

    private string timingFormat = "T";

    private bool _isInitialized;

    public ClockViewModel(ITimersService timersService)
    {
        timersService.AddUpdateTimeTimerAction(UpdateTime);
    }

    public void OnNavigatedTo(object parameter)
    {
        if (parameter is ClockWidgetSettings settings)
        {
            timingFormat = settings.ShowSeconds ? "T" : "t";
            SystemTime = DateTime.Now.ToString(timingFormat);
            _isInitialized = true;

            return;
        }

        if (!_isInitialized)
        {
            timingFormat = "T";
            SystemTime = DateTime.Now.ToString(timingFormat);
            _isInitialized = true;
        }
    }

    public void OnNavigatedFrom()
    {

    }

    private void UpdateTime(object? sender, EventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() => SystemTime = DateTime.Now.ToString(timingFormat));
    }
}
