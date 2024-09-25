using CommunityToolkit.Mvvm.ComponentModel;

namespace DesktopWidgets3.Widget.Jack251970.AnalogClock.ViewModels;

public partial class AnalogClockSettingViewModel : BaseWidgetSettingViewModel
{
    #region view properties

    [ObservableProperty]
    private bool _showSeconds = true;

    #endregion

    private AnalogClockSettings Settings = null!;

    private bool _initialized = false;

    partial void OnShowSecondsChanged(bool value)
    {
        if (_initialized)
        {
            Settings.ShowSeconds = value;
            Main.Context.WidgetService.UpdateWidgetSettings(this, Settings, true, false);
        }
    }

    #region Abstract Methods

    protected override void LoadSettings(BaseWidgetSettings settings, bool initialized)
    {
        // initialize or update properties by settings
        if (settings is AnalogClockSettings analogClockSettings)
        {
            Settings = analogClockSettings;

            ShowSeconds = Settings.ShowSeconds;

            if (!_initialized)
            {
                _initialized = true;
            }
        }
    }

    #endregion
}
