using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Timers;

namespace DesktopWidgets3.Widget.Jack251970.Network.ViewModels;

public partial class NetworkViewModel : BaseWidgetViewModel, IWidgetUpdate, IWidgetWindowClose
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

    private readonly HardwareInfoService _hardwareInfoService;

    private readonly Timer updateTimer = new();

    private bool listUpdating = false;

    public NetworkViewModel(HardwareInfoService hardwareInfoService)
    {
        _hardwareInfoService = hardwareInfoService;

        InitializeTimer(updateTimer, UpdateNetwork);
    }

    private static void InitializeTimer(Timer timer, Action action)
    {
        timer.AutoReset = true;
        timer.Enabled = false;
        timer.Interval = 1000;
        timer.Elapsed += (s, e) => action();
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
            Main.Context.WidgetService.UpdateWidgetSettings(this, newSettings, false, false);
        }
    }

    #region update methods

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
                SelectedIndex = selectedIndex;
                UploadSpeed = selectedUploadSpeed;
                DownloadSpeed = selectedDownloadSpeed;
            });
        }
        catch (Exception e)
        {
            Main.Context.LogService.LogError(ClassName, e, "Failed to update network card.");
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

    #endregion

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

            updateTimer.Start();
        }
    }

    #endregion

    #region IWidgetUpdate

    public void EnableUpdate(bool enable)
    {
        updateTimer.Enabled = enable;
    }

    #endregion

    #region IWidgetWindowClose

    public void WidgetWindowClosing()
    {
        updateTimer.Dispose();
    }

    #endregion
}
