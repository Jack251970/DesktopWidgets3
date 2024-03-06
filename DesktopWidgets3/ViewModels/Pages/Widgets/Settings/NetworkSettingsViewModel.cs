using CommunityToolkit.Mvvm.ComponentModel;

namespace DesktopWidgets3.ViewModels.Pages.Widgets.Settings;

public partial class NetworkSettingsViewModel : BaseWidgetSettingsViewModel
{
    #region view properties

    [ObservableProperty]
    private bool _useBps = false;

    #endregion

    private NetworkWidgetSettings Settings => (NetworkWidgetSettings)WidgetSettings!;

    public NetworkSettingsViewModel()
    {

    }

    protected override WidgetType InitializeWidgetType() => WidgetType.Network;

    protected override void InitializeWidgetSettings()
    {
        UseBps = Settings.UseBps;
    }

    partial void OnUseBpsChanged(bool value)
    {
        if (IsInitialized)
        {
            Settings.UseBps = value;
            NeedUpdate = true;
        }
    }
}