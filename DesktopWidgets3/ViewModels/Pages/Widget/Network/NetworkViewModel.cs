using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Contracts.Services;
using DesktopWidgets3.Models.Widget;

namespace DesktopWidgets3.ViewModels.Pages.Widget.Network;

public partial class NetworkViewModel : BaseWidgetViewModel<NetworkWidgetSettings>
{
    #region observable properties

    [ObservableProperty]
    private string _uploadSpeed = string.Empty;

    [ObservableProperty]
    private string _downloadSpeed = string.Empty;

    #endregion

    #region settings

    #endregion

    private readonly IPerformanceService _performanceService;

    public NetworkViewModel(IPerformanceService performanceService, ITimersService timersService)
    {
        _performanceService = performanceService;

        timersService.AddUpdateTimeTimerAction(UpdateNetwork);
    }

    private void UpdateNetwork(object? sender, EventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() => { 
            (UploadSpeed, DownloadSpeed) = _performanceService.GetNetworkSpeed();
            Console.WriteLine($"The Elapsed event was raised at {DateTime.Now.ToString("T")}");
        });
    }

    #region abstract methods

    protected override void LoadWidgetSettings(NetworkWidgetSettings settings)
    {
        (UploadSpeed, DownloadSpeed) = _performanceService.GetNetworkSpeed();
    }

    #endregion
}
