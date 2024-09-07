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
            var networkStats = _systemInfoService.GetNetworkStats();

            if (networkStats == null)
            {
                return;
            }

            networkSpeedInfo = GetNetworkSpeed(networkStats, useBps);
            lastNetworkNamesIdentifiers = networkNamesIdentifiers;
            networkNamesIdentifiers = networkSpeedInfo.GetHardwareNamesIdentifiers();
            (int, string, string, string, string)? networkSpeedInfoItem;
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

    private static NetworkSpeedInfo GetNetworkSpeed(NetworkStats networkStats, bool useBps)
    {
        var networkSpeedInfo = new NetworkSpeedInfo();

        var netCount = networkStats.GetNetworkCount();
        var totalSent = 0f;
        var totalReceived = 0f;
        for (var i = 0; i < netCount; i++)
        {
            var netName = networkStats.GetNetworkName(i);
            var networkUsage = networkStats.GetNetworkUsage(i);
            var uploadSpeed = FormatNetworkSpeed(networkUsage.Sent, useBps);
            var downloadSpeed = FormatNetworkSpeed(networkUsage.Received, useBps);
            networkSpeedInfo.AddItem(netName, netName, uploadSpeed, downloadSpeed);
            totalSent += networkUsage.Sent;
            totalReceived += networkUsage.Received;
        }

        var totalUploadSpeed = FormatNetworkSpeed(totalSent, useBps);
        var totalDownloadSpeed = FormatNetworkSpeed(totalReceived, useBps);
        networkSpeedInfo.InsertItem(0, "Total".GetLocalized(), "Total", totalUploadSpeed, totalDownloadSpeed);

        return networkSpeedInfo;
    }

    private static string FormatNetworkSpeed(float? bytes, bool useBps)
    {
        if (bytes is null)
        {
            return string.Empty;
        }

        var unit = useBps ? "bps" : "B/s";
        if (useBps)
        {
            bytes *= 8;
        }

        return FormatUtils.FormatBytes(bytes.Value, unit);
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

    private class NetworkSpeedInfo
    {
        private readonly List<NetworkSpeedInfoItem> NetworkSpeedInfoItems = [];

        public void AddItem(string hardwareName, string hardwareIdentifier, string uploadSpeed, string downloadSpeed)
        {
            NetworkSpeedInfoItems.Add(new NetworkSpeedInfoItem()
            {
                Name = hardwareName,
                Identifier = hardwareIdentifier,
                UploadSpeed = uploadSpeed,
                DownloadSpeed = downloadSpeed
            });
        }

        public void InsertItem(int index, string hardwareName, string hardwareIdentifier, string uploadSpeed, string downloadSpeed)
        {
            NetworkSpeedInfoItems.Insert(index, new NetworkSpeedInfoItem()
            {
                Name = hardwareName,
                Identifier = hardwareIdentifier,
                UploadSpeed = uploadSpeed,
                DownloadSpeed = downloadSpeed
            });
        }

        public (int index, string HardwareName, string HardwareIdentifier, string UploadSpeed, string DownloadSpeed)? SearchItemByIdentifier(string hardwareIdentifier)
        {
            for (var i = 0; i < NetworkSpeedInfoItems.Count; i++)
            {
                if (NetworkSpeedInfoItems[i].Identifier == hardwareIdentifier)
                {
                    return (i, NetworkSpeedInfoItems[i].Name, NetworkSpeedInfoItems[i].Identifier, NetworkSpeedInfoItems[i].UploadSpeed, NetworkSpeedInfoItems[i].DownloadSpeed);
                }
            }

            return null;
        }

        public List<Tuple<string, string>> GetHardwareNamesIdentifiers()
        {
            return NetworkSpeedInfoItems
                .Select(x => new Tuple<string, string>(x.Name, x.Identifier))
                .ToList();
        }

        private class NetworkSpeedInfoItem
        {
            public string Name { get; set; } = null!;

            public string Identifier { get; set; } = null!;

            public string UploadSpeed { get; set; } = null!;

            public string DownloadSpeed { get; set; } = null!;
        }
    }
}
