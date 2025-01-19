using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Serilog;
using System.Collections.ObjectModel;
using Timer = System.Timers.Timer;

namespace DesktopWidgets3.Widget.Jack251970.SystemInfo.ViewModels;

public partial class NetworkViewModel : ObservableRecipient
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(NetworkViewModel));

    #region view properties

    public ObservableCollection<Tuple<string, string>> NetworkNames { get; set; } =
    [
        new Tuple<string, string>("Total", "Total")
    ];

    [ObservableProperty]
    private int _selectedIndex = 0;

    [ObservableProperty]
    private string _uploadSpeed = "--";

    [ObservableProperty]
    private string _downloadSpeed = "--";

    #endregion

    #region settings

    private bool useBps = false;

    private string hardwareIdentifier = "Total";

    #endregion

    public string Id;

    private readonly DispatcherQueue _dispatcherQueue;

    private List<Tuple<string, string>> lastNetworkNamesIdentifiers = [];

    private readonly HardwareInfoService _hardwareInfoService;

    private readonly Timer updateTimer = new();

    private bool listUpdating = false;

    public NetworkViewModel(string widgetId, HardwareInfoService hardwareInfoService)
    {
        Id = widgetId;
        _dispatcherQueue = Main.WidgetInitContext.WidgetService.GetDispatcherQueue(Id);
        _hardwareInfoService = hardwareInfoService;
        InitializeTimer(updateTimer, UpdateNetwork);
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
            Main.WidgetInitContext.WidgetService.UpdateWidgetSettingsAsync(Id, newSettings);
        }
    }

    #region Timer Methods

    private void InitializeAllTimers()
    {
        updateTimer.Start();
    }

    private static void InitializeTimer(Timer timer, Action action)
    {
        timer.AutoReset = true;
        timer.Enabled = false;
        timer.Interval = 1000;
        timer.Elapsed += (s, e) => action();
    }

    public void StartAllTimers()
    {
        updateTimer.Start();
    }

    public void StopAllTimers()
    {
        updateTimer.Stop();
    }

    public void DisposeAllTimers()
    {
        updateTimer.Dispose();
    }

    #endregion

    #region Update Methods

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
                _dispatcherQueue.TryEnqueue(() =>
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

            _dispatcherQueue.TryEnqueue(() =>
            {
                SelectedIndex = selectedIndex;
                UploadSpeed = selectedUploadSpeed;
                DownloadSpeed = selectedDownloadSpeed;
            });
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to update network card.");
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

    #region Settings Methods

    public void LoadSettings(BaseWidgetSettings settings)
    {
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
    }

    #endregion
}
