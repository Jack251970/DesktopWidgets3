using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Models.Widget;

namespace DesktopWidgets3.ViewModels.Pages.Widget.Network;

public partial class NetworkViewModel : BaseWidgetViewModel<NetworkWidgetSettings>
{
    #region observable properties

    #endregion

    #region settings

    #endregion

    private readonly ITimersService _timersService;

    public NetworkViewModel(ITimersService timersService)
    {
        _timersService = timersService;

        timersService.AddUpdateTimeTimerAction(UpdateNetwork);
    }

    private void UpdateNetwork(object? sender, EventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() => { });
    }

    #region abstract methods

    protected override void LoadWidgetSettings(NetworkWidgetSettings settings)
    {
        
    }

    #endregion
}
