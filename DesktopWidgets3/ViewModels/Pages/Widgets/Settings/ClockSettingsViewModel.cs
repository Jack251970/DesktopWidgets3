                    using CommunityToolkit.Mvvm.ComponentModel;

namespace DesktopWidgets3.ViewModels.Pages.Widgets.Settings;

public partial class ClockSettingsViewModel : BaseWidgetSettingsViewModel
{
    #region view properties

    [ObservableProperty]
    private bool _showSeconds = true;

    #endregion

    private ClockWidgetSettings Settings => (ClockWidgetSettings)WidgetSettings!;

    public ClockSettingsViewModel()
    {

    }

    protected override WidgetType InitializeWidgetType() => WidgetType.Clock;

    protected override void InitializeWidgetSettings()
    {
        ShowSeconds = Settings.ShowSeconds;
    }

    partial void OnShowSecondsChanged(bool value)
    {
        if (IsInitialized)
        {
            Settings.ShowSeconds = value;
            NeedUpdate = true;
        }
    }
}
