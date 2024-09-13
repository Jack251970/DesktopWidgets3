using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWidgets3.Widget.Jack251970.AnalogClock.Setting;

namespace DesktopWidgets3.Widget.Jack251970.AnalogClock.ViewModels;

public partial class DigitalClockSettingViewModel(WidgetInitContext context) : BaseWidgetSettingViewModel
{
    #region view properties

    [ObservableProperty]
    private bool _showSeconds = true;

    #endregion

    public readonly WidgetInitContext Context = context;

    private DigitalClockSettings Settings = null!;

    private bool isInitialized = false;

    #region abstract methods

    protected override void LoadSettings(BaseWidgetSettings settings, bool initialized)
    {
        // initialize or update properties by settings
        if (settings is DigitalClockSettings digitalClockSetting)
        {
            Settings = digitalClockSetting;

            ShowSeconds = Settings.ShowSeconds;

            if (!initialized)
            {
                isInitialized = true;
            }
        }
    }

    #endregion

    partial void OnShowSecondsChanged(bool value)
    {
        if (isInitialized)
        {
            Settings.ShowSeconds = value;
            Context.API.UpdateWidgetSettings(this, Settings, true, false);
        }
    }
}
