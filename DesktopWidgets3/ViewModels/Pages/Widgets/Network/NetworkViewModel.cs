using CommunityToolkit.Mvvm.ComponentModel;

namespace DesktopWidgets3.ViewModels.Pages.Widgets;

public partial class NetworkViewModel : BaseWidgetViewModel<NetworkWidgetSettings>, IWidgetUpdate, IWidgetClose
{
    #region view properties

    [ObservableProperty]
    private string _uploadSpeed = string.Empty;

    [ObservableProperty]
    private string _downloadSpeed = string.Empty;

    #endregion

    #region settings

    private bool showBps = false;

    #endregion

    private readonly ISystemInfoService _systemInfoService;
    private readonly ITimersService _timersService;

    public NetworkViewModel(ISystemInfoService systemInfoService, ITimersService timersService)
    {
        _systemInfoService = systemInfoService;
        _timersService = timersService;

        timersService.AddTimerAction(WidgetType.Network, UpdateNetwork);
    }

    private async void UpdateNetwork()
    {
        var networkSpeed = await Task.Run(() => _systemInfoService.GetNetworkSpeed(showBps));

        RunOnDispatcherQueue(() => (UploadSpeed, DownloadSpeed) = networkSpeed);
    }

    #region abstract methods

    protected async override void LoadSettings(NetworkWidgetSettings settings)
    {
        if (settings.ShowBps != showBps)
        {
            showBps = settings.ShowBps;
        }

        if (UploadSpeed == string.Empty)
        {
            var networkSpeed = await Task.Run(() => _systemInfoService.GetInitNetworkSpeed(showBps));

            (UploadSpeed, DownloadSpeed) = networkSpeed;
        }
    }

    public override NetworkWidgetSettings GetSettings()
    {
        return new NetworkWidgetSettings
        {
            ShowBps = showBps
        };
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

    public void WidgetWindow_Closing()
    {
        _timersService.RemoveTimerAction(WidgetType.Network, UpdateNetwork);
    }

    #endregion
}
