using CommunityToolkit.Mvvm.ComponentModel;

namespace DesktopWidgets3.ViewModels.Pages.Widgets.Settings;

public partial class NetworkSettingsViewModel : BaseWidgetSettingsViewModel
{
    #region view properties

    [ObservableProperty]
    private bool _showBps = false;

    #endregion

    private NetworkWidgetSettings Settings => (NetworkWidgetSettings)WidgetSettings!;

    public NetworkSettingsViewModel()
    {

    }

    protected override WidgetType InitializeWidgetType() => WidgetType.Network;

    protected override void InitializeWidgetSettings()
    {
        ShowBps = Settings.ShowBps;
    }

    partial void OnShowBpsChanged(bool value)
    {
        if (IsInitialized)
        {
            Settings.ShowBps = value;
            NeedUpdate = true;
        }
    }
}