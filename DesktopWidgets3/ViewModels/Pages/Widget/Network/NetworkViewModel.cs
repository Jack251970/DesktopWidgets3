using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Contracts.ViewModels;
using DesktopWidgets3.Models.Widget;

namespace DesktopWidgets3.ViewModels.Pages.Widget.Network;

public partial class NetworkViewModel : BaseWidgetViewModel<NetworkWidgetSettings>, IWidgetUpdate, IWidgetClose
{
    #region observable properties

    [ObservableProperty]
    private string _uploadSpeed = string.Empty;

    [ObservableProperty]
    private string _downloadSpeed = string.Empty;

    #endregion

    #region settings

    private bool showBps = false;

    #endregion

    private readonly ISystemInfoService _performanceService;
    private readonly ITimersService _timersService;

    public NetworkViewModel(ISystemInfoService performanceService, ITimersService timersService)
    {
        _performanceService = performanceService;
        _timersService = timersService;

        timersService.AddTimerAction(WidgetType.Network, UpdateNetwork);
    }

    private void UpdateNetwork()
    {
        _dispatcherQueue.TryEnqueue(() => (UploadSpeed, DownloadSpeed) = _performanceService.GetNetworkSpeed(showBps));
    }

    #region abstract methods

    protected override void LoadWidgetSettings(NetworkWidgetSettings settings)
    {
        if (settings.ShowBps != showBps)
        {
            showBps = settings.ShowBps;
        }

        (UploadSpeed, DownloadSpeed) = _performanceService.GetNetworkSpeed(showBps);
    }

    #endregion

    #region interfaces

    public async Task EnableUpdate(bool enable)
    {
        if (enable)
        {
            _timersService.StartTimer(WidgetType.Network);
        }
        else
        {
            _timersService.StopTimer(WidgetType.Network);
        }
        await Task.CompletedTask;
    }

    public void WidgetClosed()
    {
        _timersService.RemoveTimerAction(WidgetType.Network, UpdateNetwork);
    }

    #endregion
}
