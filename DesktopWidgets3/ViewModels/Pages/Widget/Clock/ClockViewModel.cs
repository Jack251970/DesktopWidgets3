using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Models.Widget;

namespace DesktopWidgets3.ViewModels.Pages.Widget.Clock;

public partial class ClockViewModel : BaseWidgetViewModel<ClockWidgetSettings>
{
    #region observable properties

    [ObservableProperty]
    private string _systemTime = string.Empty;

    #endregion

    #region settings

    private string timingFormat = "T";

    #endregion

    private readonly ITimersService _timersService;

    public ClockViewModel(ITimersService timersService)
    {
        _timersService = timersService;

        timersService.AddUpdateTimeTimerAction(UpdateTime);
    }

    private void UpdateTime(object? sender, EventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() => SystemTime = DateTime.Now.ToString(timingFormat));
    }

    #region abstract methods

    protected override void LoadWidgetSettings(ClockWidgetSettings settings)
    {
        if (settings.ShowSeconds != (timingFormat == "T"))
        {
            timingFormat = settings.ShowSeconds ? "T" : "t";
        }

        SystemTime = DateTime.Now.ToString(timingFormat);
    }

    #endregion
}
