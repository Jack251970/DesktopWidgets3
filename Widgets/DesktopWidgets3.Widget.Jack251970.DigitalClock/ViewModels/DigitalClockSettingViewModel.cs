using CommunityToolkit.Mvvm.ComponentModel;

namespace DesktopWidgets3.Widget.Jack251970.DigitalClock.ViewModels;

public partial class DigitalClockSettingViewModel : BaseWidgetSettingViewModel
{
    #region view properties

    [ObservableProperty]
    private bool _showSeconds = true;

    #endregion

    private DigitalClockSettings Settings = null!;

    private bool _initialized = false;

    partial void OnShowSecondsChanged(bool value)
    {
        if (_initialized)
        {
            Settings.ShowSeconds = value;
            Main.Context.WidgetService.UpdateWidgetSettings(this, Settings, true, false);
        }
    }

    #region abstract methods

    protected override void LoadSettings(BaseWidgetSettings settings, bool initialized)
    {
        // initialize or update properties by settings
        if (settings is DigitalClockSettings digitalClockSettings)
        {
            Settings = digitalClockSettings;

            ShowSeconds = Settings.ShowSeconds;

            if (!_initialized)
            {
                _initialized = true;
            }
        }
    }

    #endregion
}
