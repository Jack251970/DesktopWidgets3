using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DesktopWidgets3.ViewModels.Pages.Widgets;

public partial class NetworkViewModel : BaseWidgetViewModel<NetworkWidgetSettings>, IWidgetUpdate, IWidgetClose
{
    private static string ClassName => typeof(NetworkViewModel).Name;

    #region view properties

    public ObservableCollection<Tuple<string, string>> NetworkNames { get; set; } = new();

    [ObservableProperty]
    private int _selectedIndex = 0;

    [ObservableProperty]
    private string _uploadSpeed = string.Empty;

    [ObservableProperty]
    private string _downloadSpeed = string.Empty;

    #endregion

    #region settings

    private bool useBps = false;

    private string hardwareIdentifier = "Total";

    #endregion

    private NetworkSpeedInfo networkSpeedInfo = null!;

    private List<Tuple<string, string>> lastNetworkNamesIdentifiers = [];
    private List<Tuple<string, string>> networkNamesIdentifiers = [];

    private readonly ISystemInfoService _systemInfoService;

    private bool listUpdating = false;
    private bool updating = false;

    public NetworkViewModel(ISystemInfoService systemInfoService)
    {
        _systemInfoService = systemInfoService;

        _systemInfoService.RegisterUpdatedCallback(HardwareType.Network, UpdateNetwork);
    }

    private void UpdateNetwork()
    {
        try
        {
            (int, string, string, string, string)? networkSpeedInfoItem;

            networkSpeedInfo = _systemInfoService.GetNetworkSpeed(useBps);
            lastNetworkNamesIdentifiers = networkNamesIdentifiers;
            networkNamesIdentifiers = networkSpeedInfo.GetHardwareNamesIdentifiers();
            networkSpeedInfoItem = networkSpeedInfo.SearchItemByIdentifier(hardwareIdentifier);

            if (networkSpeedInfoItem == null)
            {
                return;
            }

            if (lastNetworkNamesIdentifiers.Count != networkNamesIdentifiers.Count ||
                !lastNetworkNamesIdentifiers.SequenceEqual(networkNamesIdentifiers))
            {
                RunOnDispatcherQueue(() =>
                {
                    if (listUpdating)
                    {
                        return;
                    }

                    listUpdating = true;

                    NetworkNames.Clear();
                    foreach (var item in networkNamesIdentifiers)
                    {
                        NetworkNames.Add(item);
                    }

                    listUpdating = false;
                });
            }

            RunOnDispatcherQueue(() =>
            {
                if (updating)
                {
                    return;
                }

                updating = true;

                (var selectedIndex, _, _, var uploadSpeed, var downloadSpeed) = networkSpeedInfoItem.Value;

                SelectedIndex = selectedIndex;
                UploadSpeed = uploadSpeed;
                DownloadSpeed = downloadSpeed;

                updating = false;
            });
        }
        catch (Exception e)
        {
            LogExtensions.LogError(ClassName, e, "Failed to update network card.");
        }
    }

    partial void OnSelectedIndexChanged(int value)
    {
        if (value < 0 || value >= NetworkNames.Count)
        {
            return;
        }

        hardwareIdentifier = NetworkNames[value].Item2;

        UpdateWidgetSettings(GetSettings());
    }

    #region abstract methods

    protected override void LoadSettings(NetworkWidgetSettings settings)
    {
        if (settings.UseBps != useBps)
        {
            useBps = settings.UseBps;
        }

        if (settings.HardwareIdentifier != hardwareIdentifier)
        {
            hardwareIdentifier = settings.HardwareIdentifier;
        }

        if (UploadSpeed == string.Empty)
        {
            NetworkNames.Add(new Tuple<string, string>("Total".GetLocalized(), "Total"));
            UploadSpeed = "--";
            DownloadSpeed = "--";
        }
    }

    public override NetworkWidgetSettings GetSettings()
    {
        return new NetworkWidgetSettings
        {
            UseBps = useBps,
            HardwareIdentifier = hardwareIdentifier
        };
    }

    #endregion

    #region interfaces

    public async Task EnableUpdate(bool enable)
    {
        if (enable)
        {
            _systemInfoService.RegisterUpdatedCallback(HardwareType.Network, UpdateNetwork);
        }
        else
        {
            _systemInfoService.UnregisterUpdatedCallback(HardwareType.Network, UpdateNetwork);
        }

        await Task.CompletedTask;
    }

    public void WidgetWindow_Closing()
    {
        _systemInfoService.UnregisterUpdatedCallback(HardwareType.Network, UpdateNetwork);
    }

    #endregion
}
