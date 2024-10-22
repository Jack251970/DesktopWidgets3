using CommunityToolkit.Mvvm.ComponentModel;

namespace DesktopWidgets3.Widget.Jack251970.Clock.ViewModels;

public partial class DigitalClockSettingViewModel(string widgetId) : ObservableRecipient
{
    #region view properties

    [ObservableProperty]
    private bool _showSeconds = true;

    #endregion

    public string Id = widgetId;

    private DigitalClockSettings Settings = null!;

    private bool _initialized = false;

    partial void OnShowSecondsChanged(bool value)
    {
        if (_initialized)
        {
            Settings.ShowSeconds = value;
            Main.WidgetInitContext.WidgetService.UpdateWidgetSettingsAsync(Id, Settings);
        }
    }

    #region Settings Methods

    public void LoadSettings(BaseWidgetSettings settings)
    {
        if (settings is DigitalClockSettings digitalClockSettings)
        {
            // initialize settings instance
            if (!_initialized)
            {
                Settings = digitalClockSettings;
                _initialized = true;
            }

            // update settings
            ShowSeconds = Settings.ShowSeconds;
        }
    }

    #endregion
}
