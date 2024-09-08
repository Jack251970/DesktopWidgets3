using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DesktopWidgets3.ViewModels.Pages.Widgets;

public partial class NetworkViewModel : BaseWidgetViewModel<NetworkWidgetSettings>, IWidgetUpdate, IWidgetClose
{
    private static string ClassName => typeof(NetworkViewModel).Name;

    #region view properties

    public ObservableCollection<Tuple<string, string>> NetworkNames { get; set; } = [];

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

    private List<Tuple<string, string>> lastNetworkNamesIdentifiers = [];

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

            var networkNamesIdentifiers = new List<Tuple<string, string>>();
            var selectedIndex = -1;
            var selectedUploadSpeed = "--";
            var selectedDownloadSpeed = "--";

            var totalSent = 0f;
            var totalReceived = 0f;
            for (var i = 0; i < networkStats.GetNetworkCount(); i++)
            {
                var netName = networkStats.GetNetworkName(i);
                var networkUsage = networkStats.GetNetworkUsage(i);
                totalSent += networkUsage.Sent;
                totalReceived += networkUsage.Received;

                networkNamesIdentifiers.Add(new Tuple<string, string>(netName, netName));
                if (hardwareIdentifier != "Total" && netName == hardwareIdentifier)
                {
                    selectedIndex = i + 1;
                    selectedUploadSpeed = FormatNetworkSpeed(networkUsage.Sent, useBps);
                    selectedDownloadSpeed = FormatNetworkSpeed(networkUsage.Received, useBps);
                }
            }

            networkNamesIdentifiers.Insert(0, new Tuple<string, string>("Total".GetLocalized(), "Total"));
            if (hardwareIdentifier == "Total")
            {
                selectedIndex = 0;
                selectedUploadSpeed = FormatNetworkSpeed(totalSent, useBps);
                selectedDownloadSpeed = FormatNetworkSpeed(totalReceived, useBps);
            }

            if (selectedIndex == -1)
            {
                return;
            }

            if (lastNetworkNamesIdentifiers.Count != networkNamesIdentifiers.Count ||
                !lastNetworkNamesIdentifiers.SequenceEqual(networkNamesIdentifiers))
            {
                TryEnqueue(() =>
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

            lastNetworkNamesIdentifiers = networkNamesIdentifiers;

            TryEnqueue(() =>
            {
                if (updating)
                {
                    return;
                }

                updating = true;

                SelectedIndex = selectedIndex;
                UploadSpeed = selectedUploadSpeed;
                DownloadSpeed = selectedDownloadSpeed;

                updating = false;
            });
        }
        catch (Exception e)
        {
            LogExtensions.LogError(ClassName, e, "Failed to update network card.");
        }
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
            if (hardwareIdentifier == "Total")
            {
                NetworkNames.Add(new Tuple<string, string>("Total".GetLocalized(), "Total"));
            }
            else
            {
                NetworkNames.Add(new Tuple<string, string>(hardwareIdentifier, hardwareIdentifier));
            }
            SelectedIndex = 0;
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
