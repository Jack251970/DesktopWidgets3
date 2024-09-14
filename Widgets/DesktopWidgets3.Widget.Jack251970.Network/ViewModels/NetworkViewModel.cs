using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace DesktopWidgets3.Widget.Jack251970.Network.ViewModels;

public partial class NetworkViewModel : BaseWidgetViewModel, IWidgetUpdate, IWidgetClosing
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

    private readonly WidgetInitContext Context;

    private readonly HardwareInfoService _hardwareInfoService;

    private bool listUpdating = false;
    private bool updating = false;

    public NetworkViewModel(WidgetInitContext context, HardwareInfoService hardwareInfoService)
    {
        Context = context;

        _hardwareInfoService = hardwareInfoService;
    }

    private void UpdateNetwork()
    {
        try
        {
            var networkStats = _hardwareInfoService.GetNetworkStats();

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

            networkNamesIdentifiers.Insert(0, new Tuple<string, string>("Total", "Total"));
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
                DispatcherQueue.TryEnqueue(() =>
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

            DispatcherQueue.TryEnqueue(() =>
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
            Context.API.LogError(ClassName, e, "Failed to update network card.");
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

        var newHardwareIdentifier = NetworkNames[value].Item2;

        if (hardwareIdentifier != newHardwareIdentifier)
        {
            hardwareIdentifier = newHardwareIdentifier;
            var newSettings = new NetworkSettings()
            {
                UseBps = useBps,
                HardwareIdentifier = hardwareIdentifier
            };
            Context.API.UpdateWidgetSettings(this, newSettings, false, true);
        }
    }

    #region abstract methods

    protected override void LoadSettings(BaseWidgetSettings settings, bool initialized)
    {
        // initialize or update widget from settings
        if (settings is NetworkSettings networkSettings)
        {
            if (networkSettings.UseBps != useBps)
            {
                useBps = networkSettings.UseBps;
            }

            if (networkSettings.HardwareIdentifier != hardwareIdentifier)
            {
                hardwareIdentifier = networkSettings.HardwareIdentifier;
            }
        }

        // initialize widget
        if (initialized)
        {
            if (hardwareIdentifier == "Total")
            {
                NetworkNames.Add(new Tuple<string, string>("Total", "Total"));
            }
            else
            {
                NetworkNames.Add(new Tuple<string, string>(hardwareIdentifier, hardwareIdentifier));
            }
            SelectedIndex = 0;
            UploadSpeed = "--";
            DownloadSpeed = "--";

            _hardwareInfoService.RegisterUpdatedCallback(HardwareType.Network, UpdateNetwork);
        }
    }

    #endregion

    #region widget update

    public async Task EnableUpdate(bool enable)
    {
        if (enable)
        {
            _hardwareInfoService.RegisterUpdatedCallback(HardwareType.Network, UpdateNetwork);
        }
        else
        {
            _hardwareInfoService.UnregisterUpdatedCallback(HardwareType.Network, UpdateNetwork);
        }

        await Task.CompletedTask;
    }

    #endregion

    #region widget closing

    public void WidgetWindow_Closing()
    {
        _hardwareInfoService.UnregisterUpdatedCallback(HardwareType.Network, UpdateNetwork);
    }

    #endregion
}
