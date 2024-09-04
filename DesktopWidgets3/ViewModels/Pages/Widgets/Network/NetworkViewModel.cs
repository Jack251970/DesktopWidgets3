﻿using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DesktopWidgets3.ViewModels.Pages.Widgets;

public partial class NetworkViewModel : BaseWidgetViewModel<NetworkWidgetSettings>, IWidgetUpdate, IWidgetClose
{
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

    private List<Tuple<string, string>> lastNetworkNamesIdentifiers = new();
    private List<Tuple<string, string>> networkNamesIdentifiers = new();

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
        await UpdateCard(false);
    }

    private async Task UpdateCard(bool init)
    {
        (int, string, string, string, string)? networkSpeedInfoItem;

        if (init)
        {
            networkSpeedInfo = await Task.Run(() => _systemInfoService.GetInitNetworkSpeed(useBps));
            lastNetworkNamesIdentifiers = networkNamesIdentifiers;
            networkNamesIdentifiers = networkSpeedInfo.GetHardwareNamesIdentifiers();
            networkSpeedInfoItem = networkSpeedInfo.GetItem(0);
        }
        else
        {
            networkSpeedInfo = await Task.Run(() => _systemInfoService.GetNetworkSpeed(useBps));
            lastNetworkNamesIdentifiers = networkNamesIdentifiers;
            networkNamesIdentifiers = networkSpeedInfo.GetHardwareNamesIdentifiers();
            networkSpeedInfoItem = networkSpeedInfo.SearchItemByIdentifier(hardwareIdentifier);
        }

        if (lastNetworkNamesIdentifiers.Count != networkNamesIdentifiers.Count || 
            !lastNetworkNamesIdentifiers.SequenceEqual(networkNamesIdentifiers))
        {
            RunOnDispatcherQueue(() =>
            {
                NetworkNames.Clear();
                foreach (var item in networkNamesIdentifiers)
                {
                    NetworkNames.Add(item);
                }
            });
        }

        var selectedIndex = 0;
        var uploadSpeed = string.Empty;
        var downloadSpeed = string.Empty;
        if (networkSpeedInfoItem != null)
        {
            (selectedIndex, _, _, uploadSpeed, downloadSpeed) = networkSpeedInfoItem.Value;
        }

        RunOnDispatcherQueue(() => {
            SelectedIndex = selectedIndex;
            UploadSpeed = uploadSpeed;
            DownloadSpeed = downloadSpeed;
        });
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

    protected async override void LoadSettings(NetworkWidgetSettings settings)
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
            await UpdateCard(true);
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
