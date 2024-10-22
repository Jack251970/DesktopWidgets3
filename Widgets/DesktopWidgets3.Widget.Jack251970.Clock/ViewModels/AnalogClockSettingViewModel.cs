using CommunityToolkit.Mvvm.ComponentModel;

namespace DesktopWidgets3.Widget.Jack251970.Clock.ViewModels;

public partial class AnalogClockSettingViewModel(string widgetId) : ObservableRecipient
{
    #region view properties

    [ObservableProperty]
    private bool _showSeconds = true;

    #endregion

    public string Id = widgetId;

    private AnalogClockSettings Settings = null!;

    private bool _initialized = false;

    partial void OnShowSecondsChanged(bool value)
    {
        if (_initialized)
        {
            Settings.ShowSeconds = value;
            Main.WidgetInitContext.WidgetService.UpdateWidgetSettingsAsync(Id, Settings);
        }
    }

    #region Abstract Methods

    public void LoadSettings(BaseWidgetSettings settings)
    {
        if (settings is AnalogClockSettings analogClockSettings)
        {
            // initialize settings instance
            if (!_initialized)
            {
                Settings = analogClockSettings;
                _initialized = true;
            }

            // update settings
            ShowSeconds = Settings.ShowSeconds;
        }
    }

    #endregion
}
